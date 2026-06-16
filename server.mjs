// 服务端 MathJax HTML 公式渲染接口
// 运行: node server.mjs  或  pm2 start ecosystem.config.cjs

import express from 'express';
import sharp from 'sharp';
import { createHash } from 'node:crypto';
import { LRUCache } from 'lru-cache';
import { mathjax } from 'mathjax-full/js/mathjax.js';
import { TeX } from 'mathjax-full/js/input/tex.js';
import { MathML as MathMLInput } from 'mathjax-full/js/input/mathml.js';
import { CHTML } from 'mathjax-full/js/output/chtml.js';
import { SVG } from 'mathjax-full/js/output/svg.js';
import { liteAdaptor } from 'mathjax-full/js/adaptors/liteAdaptor.js';
import { RegisterHTMLHandler } from 'mathjax-full/js/handlers/html.js';
import { AllPackages } from 'mathjax-full/js/input/tex/AllPackages.js';
import { SerializedMmlVisitor } from 'mathjax-full/js/core/MmlTree/SerializedMmlVisitor.js';
import { STATE } from 'mathjax-full/js/core/MathItem.js';

// ------------------------------------------------------------------
// 0. 配置
// ------------------------------------------------------------------
const RENDER_TIMEOUT_MS = 10_000;        // 渲染超时（10秒）
const CACHE_MAX_ITEMS = 2000;             // LRU 最大缓存条目数
const CACHE_TTL_MS = 30 * 60 * 1000;      // 缓存 TTL（30 分钟）
const MAX_CONCURRENT_RENDERS = process.env.MAX_CONCURRENT || 10; // 最大并发渲染数

// ------------------------------------------------------------------
// 0.1 LRU 缓存
// ------------------------------------------------------------------
const cache = new LRUCache({
    max: CACHE_MAX_ITEMS,
    ttl: CACHE_TTL_MS,
});
let cacheHits = 0;
let cacheMisses = 0;

/**
 * 生成缓存 key（使用 SHA-256 哈希，节省内存）
 */
function cacheKey(prefix, content, opts = {}) {
    const raw = `${prefix}:${JSON.stringify(opts)}:${content}`;
    return createHash('sha256').update(raw).digest('hex');
}

// ------------------------------------------------------------------
// 0.1.1 并发限流信号量
// ------------------------------------------------------------------
let _concurrentCount = 0;   // 当前正在渲染的请求数
let _waitingQueue = [];     // 排队中的请求
let _totalQueued = 0;       // 历史总排队数（统计用）

/**
 * 获取渲染许可；当并发数已满时排队等待
 * @returns {Promise<void>}
 */
function acquireRenderSlot() {
    if (_concurrentCount < MAX_CONCURRENT_RENDERS) {
        _concurrentCount++;
        return Promise.resolve();
    }
    _totalQueued++;
    return new Promise(resolve => {
        _waitingQueue.push(resolve);
    });
}

/**
 * 释放渲染许可；唤醒排队中的下一个请求
 */
function releaseRenderSlot() {
    if (_waitingQueue.length > 0) {
        const next = _waitingQueue.shift();
        // 不减 _concurrentCount，直接移交给下一个
        next();
    } else {
        _concurrentCount--;
    }
}

// ------------------------------------------------------------------
// 0.2 超时保护
// ------------------------------------------------------------------
function withTimeout(fn, timeoutMs = RENDER_TIMEOUT_MS) {
    return new Promise((resolve, reject) => {
        const timer = setTimeout(() => {
            reject(new Error(`渲染超时（超过 ${timeoutMs}ms）`));
        }, timeoutMs);

        try {
            const result = fn();
            // 支持同步/异步函数
            if (result && typeof result.then === 'function') {
                result.then(v => { clearTimeout(timer); resolve(v); })
                    .catch(e => { clearTimeout(timer); reject(e); });
            } else {
                clearTimeout(timer);
                resolve(result);
            }
        } catch (e) {
            clearTimeout(timer);
            reject(e);
        }
    });
}

// ------------------------------------------------------------------
// 1. 初始化 MathJax（只做一次，进程级单例）
// ------------------------------------------------------------------
const adaptor = liteAdaptor();
RegisterHTMLHandler(adaptor);

// TeX 输入
const texInput = new TeX({
    packages: AllPackages,
    inlineMath: [['$', '$'], ['\\(', '\\)']],
    displayMath: [['$$', '$$'], ['\\[', '\\]']],
    processEscapes: true,
    processEnvironments: true,
    processRefs: true,
});

// MathML 输入（用于 mml -> chtml/svg）
const mmlInput = new MathMLInput();

// CHTML 输出（字体使用 CDN，避免本地字体路径问题）
const chtmlOutput = new CHTML({
    fontURL: 'https://cdn.jsdelivr.net/npm/mathjax@3/es5/output/chtml/fonts/woff-v2',
    adaptiveCSS: true,
});

// SVG 输出
const svgOutput = new SVG({
    fontCache: 'local', // 'local' | 'global' | 'none'
});

// MathML 序列化器（TeX -> MathML 字符串走 visitor，不需要 OutputJax）
const mmlVisitor = new SerializedMmlVisitor();
const toMmlString = (node) => mmlVisitor.visitTree(node);

// 单公式渲染：document 可复用空文档（每次 convert 一段 TeX）
const docCHTML = mathjax.document('', { InputJax: texInput, OutputJax: chtmlOutput });
const docSVG = mathjax.document('', { InputJax: texInput, OutputJax: svgOutput });
const docMmlToChtml = mathjax.document('', { InputJax: mmlInput, OutputJax: chtmlOutput });

// 用于获取 CHTML 全量样式的载体 document（renderHtml 走临时 document，不会更新 docCHTML 的 adaptiveCSS）
const styleDocCHTML = mathjax.document('', { InputJax: texInput, OutputJax: chtmlOutput });

// ------------------------------------------------------------------
// 2. 渲染函数
// ------------------------------------------------------------------

/**
 * 渲染整段 HTML 中的所有 TeX 公式
 * @param {string} htmlString 含 TeX 公式的 HTML
 * @param {'chtml'|'svg'|'mml'} format 输出格式
 * @param {boolean} embedStyle 是否在结果开头嵌入必需的样式（CHTML 必备）
 */
function renderHtml(htmlString, format = 'mml', embedStyle = false) {
    // ---- MathML 模式：保留 HTML 结构，把每个 TeX 公式替换为 <math>...</math> ----
    if (format === 'mml') {
        // 检查是否包含 TeX 公式（$...$, $$...$$, \(...\), \[...\]）
        const hasTexFormula = /\$[\s\S]*?\$|\\\([\s\S]*?\\\)|\\\[[\s\S]*?\\\]/.test(htmlString);
        
        if (!hasTexFormula) {
            // 没有 TeX 公式，直接返回原始内容（可能包含纯 MathML 标签）
            return htmlString;
        }

        // 尝试 MML 模式
        try {
            // OutputJax 仅作占位（不会用其 typeset 输出）
            const doc = mathjax.document(htmlString, {
                InputJax: texInput,
                OutputJax: chtmlOutput,
            });

            // 1) 找出所有 TeX
            doc.findMath().compile();

            // 如果没有找到任何数学公式，直接返回原始内容
            if (doc.math.length === 0) {
                return htmlString;
            }

            // 2) 把每个 MathItem 的 root 序列化为 <math> 节点，赋给 typesetRoot
            for (const item of doc.math) {
                const mml = mmlVisitor.visitTree(item.root, doc);
                const tmpDoc = adaptor.parse(mml, 'text/html');
                const mathNode = adaptor.firstChild(adaptor.body(tmpDoc));
                item.typesetRoot = mathNode;
                item.state(STATE.TYPESET);
            }

            // 3) 用 typesetRoot 替换原 TeX 文本
            doc.updateDocument();

            return adaptor.innerHTML(adaptor.body(doc.document));
        } catch (err) {
            // MML 模式解析失败，尝试用 CHTML 模式渲染（公式会渲染但 HTML 可能变化）
            console.error('MML 模式渲染失败，使用 CHTML fallback:', err.message);
            try {
                const doc = mathjax.document(htmlString, {
                    InputJax: texInput,
                    OutputJax: chtmlOutput,
                });
                doc.render();
                const body = adaptor.body(doc.document);
                let out = adaptor.innerHTML(body);
                if (embedStyle) {
                    const styleSheet = adaptor.outerHTML(chtmlOutput.styleSheet(doc));
                    out = styleSheet + '\n' + out;
                }
                return out;
            } catch (fallbackErr) {
                // CHTML 模式也失败，返回原始内容
                console.error('CHTML fallback 也失败:', fallbackErr.message);
                return htmlString;
            }
        }
    }

    // ---- chtml / svg 模式：保留原行为 ----
    const outputJax = format === 'svg' ? svgOutput : chtmlOutput;
    const doc = mathjax.document(htmlString, {
        InputJax: texInput,
        OutputJax: outputJax,
    });
    doc.render();

    const body = adaptor.body(doc.document);
    let out = adaptor.innerHTML(body);

    if (embedStyle) {
        const styleSheet = adaptor.outerHTML(outputJax.styleSheet(doc));
        out = styleSheet + '\n' + out;
    }
    return out;
}

/**
 * 渲染单个 TeX 公式
 */
function renderTex(tex, { display = false, format = 'chtml', embedStyle = false } = {}) {
    // 走 MathML 字符串
    if (format === 'mml') {
        const node = docCHTML.convert(tex, { display, end: ['parse'] });
        return toMmlString(node);
    }
    const doc = format === 'svg' ? docSVG : docCHTML;
    const node = doc.convert(tex, {
        display,
        em: 16,
        ex: 8,
        containerWidth: 80 * 16,
    });

    let html = adaptor.outerHTML(node);
    if (embedStyle) {
        const styleSheet = adaptor.outerHTML(doc.outputJax.styleSheet(doc));
        html = styleSheet + '\n' + html;
    }
    return html;
}

/**
 * 渲染 MathML -> CHTML
 */
function renderMathML(mmlString, embedStyle = false) {
    const node = docMmlToChtml.convert(mmlString, {
        display: false,
        em: 16,
        ex: 8,
        containerWidth: 80 * 16,
    });
    let html = adaptor.outerHTML(node);
    if (embedStyle) {
        const styleSheet = adaptor.outerHTML(docMmlToChtml.outputJax.styleSheet(docMmlToChtml));
        html = styleSheet + '\n' + html;
    }
    return html;
}

/**
 * 单独获取 CHTML 当前累积样式（adaptiveCSS=true 时随渲染而增长）
 */
function getChtmlStyleSheet() {
    return adaptor.outerHTML(chtmlOutput.styleSheet(styleDocCHTML));
}

/**
 * 将单个 TeX 公式渲染为 PNG Buffer
 * @param {string} tex LaTeX 表达式
 * @param {object} options
 * @param {boolean} options.display 是否块级公式
 * @param {number} options.scale 放大倍数（控制 PNG 分辨率，默认 2）
 * @returns {Promise<Buffer>} PNG 图片 Buffer
 */
async function renderTexToPng(tex, { display = false, scale = 2 } = {}) {
    // 1) TeX -> SVG (convert 返回 <mjx-container><svg>...</svg></mjx-container>)
    const svgNode = docSVG.convert(tex, {
        display,
        em: 16,
        ex: 8,
        containerWidth: 80 * 16,
    });
    // 取内层 <svg> 元素（sharp 需要纯 <svg> 根节点）
    const svgElement = adaptor.firstChild(svgNode);
    let svgString = adaptor.outerHTML(svgElement);

    // 2) 从 SVG 中提取原始尺寸（ex 单位）
    const widthMatch = svgString.match(/width="([\d.]+)ex"/);
    const heightMatch = svgString.match(/height="([\d.]+)ex"/);
    const exToPx = 8; // 1ex ≈ 8px
    const width = widthMatch ? Math.ceil(parseFloat(widthMatch[1]) * exToPx * scale) : 300;
    const height = heightMatch ? Math.ceil(parseFloat(heightMatch[1]) * exToPx * scale) : 100;

    // 3) 给 SVG 加上固定 px 尺寸，确保 sharp 能正确渲染
    svgString = svgString
        .replace(/width="[^"]*"/, `width="${width}"`)
        .replace(/height="[^"]*"/, `height="${height}"`);

    // 4) SVG -> PNG
    const pngBuffer = await sharp(Buffer.from(svgString))
        .png({ quality: 100 })
        .toBuffer();

    return pngBuffer;
}

/**
 * 将 HTML 中的所有 TeX 公式替换为 PNG 图片（base64 data URI 或文件路径）
 * @param {string} htmlString 含 TeX 公式的 HTML
 * @param {object} options
 * @param {number} options.scale 放大倍数
 * @param {'dataUri'|'buffer'} options.mode dataUri=返回嵌入 base64 的 <img>; buffer=返回图片列表
 * @returns {Promise<string|object>}
 */
async function renderHtmlWithPng(htmlString, { scale = 2, mode = 'dataUri' } = {}) {
    // 用 MathJax 解析 HTML 找到所有公式
    const doc = mathjax.document(htmlString, {
        InputJax: texInput,
        OutputJax: svgOutput,
    });
    doc.findMath().compile();

    const formulaImages = []; // mode=buffer 时收集

    // 对每个公式生成 PNG
    for (const item of doc.math) {
        const texStr = item.math; // 原始 TeX 源码
        const isDisplay = item.display;

        // TeX -> SVG -> PNG
        const svgNode = docSVG.convert(texStr, {
            display: isDisplay,
            em: 16, ex: 8, containerWidth: 80 * 16,
        });
        // 取内层 <svg>
        const svgElement = adaptor.firstChild(svgNode);
        let svgString = adaptor.outerHTML(svgElement);

        const widthMatch = svgString.match(/width="([\d.]+)ex"/);
        const heightMatch = svgString.match(/height="([\d.]+)ex"/);
        const exToPx = 8;
        const width = widthMatch ? Math.ceil(parseFloat(widthMatch[1]) * exToPx * scale) : 300;
        const height = heightMatch ? Math.ceil(parseFloat(heightMatch[1]) * exToPx * scale) : 100;

        svgString = svgString
            .replace(/width="[^"]*"/, `width="${width}"`)
            .replace(/height="[^"]*"/, `height="${height}"`);

        const pngBuffer = await sharp(Buffer.from(svgString))
            .png({ quality: 100 })
            .toBuffer();

        if (mode === 'dataUri') {
            const base64 = pngBuffer.toString('base64');
            const imgTag = `<img src="data:image/png;base64,${base64}" alt="${texStr.replace(/"/g, '&quot;')}" style="vertical-align: middle;${isDisplay ? ' display: block; margin: 0 auto;' : ''}" />`;
            // 设为 typesetRoot
            const tmpDoc = adaptor.parse(imgTag, 'text/html');
            const imgNode = adaptor.firstChild(adaptor.body(tmpDoc));
            item.typesetRoot = imgNode;
            item.state(STATE.TYPESET);
        } else {
            formulaImages.push({
                tex: texStr,
                display: isDisplay,
                png: pngBuffer.toString('base64'),
                width, height,
            });
        }
    }

    if (mode === 'dataUri') {
        doc.updateDocument();
        return { html: adaptor.innerHTML(adaptor.body(doc.document)) };
    } else {
        return { html: htmlString, images: formulaImages };
    }
}

// ------------------------------------------------------------------
// 3. Express 接口
// ------------------------------------------------------------------
const app = express();
app.use(express.json({ limit: '30mb' }));
app.use(express.urlencoded({ extended: true, limit: '30mb' }));

// 健康检查
app.get('/', (_req, res) => {
    res.type('text/plain').send(`MathJax 公式渲染服务运行中 (PID: ${process.pid})`);
});

// 缓存 & 并发统计
app.get('/cache-stats', (_req, res) => {
    const total = cacheHits + cacheMisses;
    res.json({
        size: cache.size,
        maxSize: CACHE_MAX_ITEMS,
        hits: cacheHits,
        misses: cacheMisses,
        hitRate: total > 0 ? (cacheHits / total * 100).toFixed(2) + '%' : '0%',
        concurrency: {
            active: _concurrentCount,
            queued: _waitingQueue.length,
            maxConcurrent: MAX_CONCURRENT_RENDERS,
            totalQueued: _totalQueued,
        },
    });
});

/**
 * 渲染整段 HTML（推荐主接口）
 * POST /render-math
 * body: { html: string, format?: 'chtml'|'svg'|'mml', embedStyle?: boolean }
 */
app.post('/render-math', async (req, res) => {
    const { html, format = 'mml', embedStyle = false, useCache = true } = req.body || {};
    if (typeof html !== 'string' || !html) {
        return res.status(400).json({ error: '请传入 html 参数（字符串）' });
    }
    // 缓存命中直接返回（不占并发槽位）
    if (useCache) {
        const key = cacheKey('html', html, { format, embedStyle });
        const cached = cache.get(key);
        if (cached) { cacheHits++; return res.type('text/html; charset=utf-8').send(cached); }
    }
    // 获取渲染槽位
    await acquireRenderSlot();
    try {
        const result = await withTimeout(() => renderHtml(html, format, embedStyle));
        if (useCache) { cacheMisses++; cache.set(cacheKey('html', html, { format, embedStyle }), result); }
        res.type('text/html; charset=utf-8').send(result);
    } catch (err) {
        console.error('renderHtml 错误:', err);
        res.status(err.message.includes('超时') ? 504 : 500).json({ error: err.message });
    } finally {
        releaseRenderSlot();
    }
});

/**
 * 渲染单个 TeX 公式
 * POST /render-tex
 */
app.post('/render-tex', async (req, res) => {
    const { tex, display = false, format = 'chtml', embedStyle = false, useCache = true } = req.body || {};
    if (typeof tex !== 'string' || !tex) {
        return res.status(400).json({ error: '请传入 tex 参数（字符串）' });
    }
    if (useCache) {
        const key = cacheKey('tex', tex, { display, format, embedStyle });
        const cached = cache.get(key);
        if (cached) { cacheHits++; return res.type('text/html; charset=utf-8').send(cached); }
    }
    await acquireRenderSlot();
    try {
        const result = await withTimeout(() => renderTex(tex, { display, format, embedStyle }));
        if (useCache) { cacheMisses++; cache.set(cacheKey('tex', tex, { display, format, embedStyle }), result); }
        res.type('text/html; charset=utf-8').send(result);
    } catch (err) {
        console.error('renderTex 错误:', err);
        res.status(err.message.includes('超时') ? 504 : 500).json({ error: err.message });
    } finally {
        releaseRenderSlot();
    }
});

/**
 * 渲染 MathML -> CHTML
 * POST /render-mml
 */
app.post('/render-mml', async (req, res) => {
    const { mml, embedStyle = false, useCache = true } = req.body || {};
    if (typeof mml !== 'string' || !mml) {
        return res.status(400).json({ error: '请传入 mml 参数（字符串）' });
    }
    if (useCache) {
        const key = cacheKey('mml', mml, { embedStyle });
        const cached = cache.get(key);
        if (cached) { cacheHits++; return res.type('text/html; charset=utf-8').send(cached); }
    }
    await acquireRenderSlot();
    try {
        const result = await withTimeout(() => renderMathML(mml, embedStyle));
        if (useCache) { cacheMisses++; cache.set(cacheKey('mml', mml, { embedStyle }), result); }
        res.type('text/html; charset=utf-8').send(result);
    } catch (err) {
        console.error('renderMathML 错误:', err);
        res.status(err.message.includes('超时') ? 504 : 500).json({ error: err.message });
    } finally {
        releaseRenderSlot();
    }
});

/**
 * 单独获取当前 CHTML 累积样式
 * GET /chtml-style
 */
app.get('/chtml-style', (_req, res) => {
    res.type('text/html; charset=utf-8').send(getChtmlStyleSheet());
});

/**
 * 单个公式渲染为 PNG 图片
 * POST /render-tex-png
 */
app.post('/render-tex-png', async (req, res) => {
    const { tex, display = false, scale = 2, useCache = true } = req.body || {};
    if (typeof tex !== 'string' || !tex) {
        return res.status(400).json({ error: '请传入 tex 参数（字符串）' });
    }
    if (useCache) {
        const key = cacheKey('tex-png', tex, { display, scale });
        const cached = cache.get(key);
        if (cached) { cacheHits++; return res.type('image/png').send(cached); }
    }
    await acquireRenderSlot();
    try {
        const pngBuffer = await withTimeout(() => renderTexToPng(tex, { display, scale }));
        if (useCache) { cacheMisses++; cache.set(cacheKey('tex-png', tex, { display, scale }), pngBuffer); }
        res.type('image/png').send(pngBuffer);
    } catch (err) {
        console.error('renderTexToPng 错误:', err);
        res.status(err.message.includes('超时') ? 504 : 500).json({ error: err.message });
    } finally {
        releaseRenderSlot();
    }
});

/**
 * 整段 HTML 公式替换为 PNG
 * POST /render-math-png
 */
app.post('/render-math-png', async (req, res) => {
    const { html, scale = 2, useCache = true } = req.body || {};
    if (typeof html !== 'string' || !html) {
        return res.status(400).json({ error: '请传入 html 参数（字符串）' });
    }
    if (useCache) {
        const key = cacheKey('html-png', html, { scale });
        const cached = cache.get(key);
        if (cached) { cacheHits++; return res.json(cached); }
    }
    await acquireRenderSlot();
    try {
        const result = await withTimeout(() => renderHtmlWithPng(html, { scale, mode: 'dataUri' }));
        if (useCache) { cacheMisses++; cache.set(cacheKey('html-png', html, { scale }), result); }
        res.json(result);
    } catch (err) {
        console.error('renderHtmlWithPng 错误:', err);
        res.status(err.message.includes('超时') ? 504 : 500).json({ error: err.message });
    } finally {
        releaseRenderSlot();
    }
});

// ------------------------------------------------------------------
// 4. 启动
// ------------------------------------------------------------------
const PORT = process.env.PORT ? Number(process.env.PORT) : 6677;
app.listen(PORT, "0.0.0.0",() => {
    console.log(`✅ MathJax 渲染服务启动: http://localhost:${PORT} (PID: ${process.pid})`);
    console.log(`   POST /render-math      渲染整段 HTML`);
    console.log(`   POST /render-tex       渲染单个 TeX 公式`);
    console.log(`   POST /render-mml       渲染 MathML`);
    console.log(`   POST /render-tex-png   公式→PNG`);
    console.log(`   POST /render-math-png  HTML公式→PNG`);
    console.log(`   GET  /cache-stats      缓存统计`);
    console.log(`   GET  /chtml-style      获取 CHTML 样式`);
    // 供 .NET 宿主进程识别服务就绪与端口
    console.log(`MATHJAX_SERVICE_READY:${PORT}`);
});

// 优雅关闭支持（PM2 graceful stop）
process.on('SIGINT', () => {
    console.log('\n正在关闭 MathJax 服务...');
    process.exit(0);
});
process.on('SIGTERM', () => {
    console.log('\n收到 SIGTERM，关闭 MathJax 服务...');
    process.exit(0);
});
