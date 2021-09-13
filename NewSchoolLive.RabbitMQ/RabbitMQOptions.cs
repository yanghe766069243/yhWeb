

namespace NewSchoolLive.RabbitMQ
{ 
    public class RabbitMQOptions
    {

        public string HostName { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string VirtualHost { get; set; }

    }

    public class RabbitMQConsumerModel
    {
        /// <summary>
        /// 生产者指定，交换机
        /// </summary>
        public string ExchangeName { get; set; }
        /// <summary>
        /// 自己起的名字
        /// </summary>
        public string QueueName { get; set; }
    }

    public class RabbitMQExchangeQueueName
    {

        /// <summary>
        /// 订单创建后的交换机
        /// </summary>
        public static readonly string OrderCreate_Exchange = "MSACormmerce.OrderCreate.Exchange";
        public static readonly string OrderCreate_Queue_CleanCart = "MSACormmerce.OrderCreate.Queue.CleanCart";

        /// <summary>
        /// 订单创建后的交换机,支付状态的
        /// </summary>
        public static readonly string OrderPay_Exchange = "MSACormmerce.OrderPay.Exchange";
        public static readonly string OrderPay_Queue_RefreshPay = "MSACormmerce.OrderPay.Queue.RefreshPay";

        /// <summary>
        /// 创建订单后的延时队列配置
        /// </summary>
        public static readonly string OrderCreate_Delay_Exchange = "MSACormmerce.OrderCreate.DelayExchange";
        public static readonly string OrderCreate_Delay_Queue_CancelOrder = "MSACormmerce.OrderCreate.DelayQueue.CancelOrder";

        /// <summary>
        /// 秒杀异步的
        /// </summary>
        public static readonly string Seckill_Exchange = "MSACormmerce.Seckill.Exchange";
        public static readonly string Seckill_Order_Queue = "MSACormmerce.Seckill.Order.Queue";


        /// <summary>
        /// CAP队列名称
        /// </summary>
        public const string Order_Stock_Decrease = "RabbitMQ.MySQL.Order-Stock.Decrease";
        public const string Order_Stock_Resume = "RabbitMQ.MySQL.Order-Stock.Resume";
        public const string Stock_Logistics = "RabbitMQ.MySQL.Stock-Logistics";

        public const string Pay_Order_UpdateStatus = "RabbitMQ.MySQL.Pay_Order.UpdateStatus";


    }

}

