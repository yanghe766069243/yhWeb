using System;
using System.Linq;
using System.Text;

namespace NewSchoolLive.Model
{
    ///<summary>
    ///用户表
    ///</summary>
    public partial class OS_User 
    {
        public OS_User()
        {
            this.Rating = Convert.ToInt32("1");
            this.GraduationYear = Convert.ToString("");
        }
        /// <summary>
        /// Desc:编号
        /// Default:
        /// Nullable:False
        /// </summary>           
        public long Id { get; set; }
        /// <summary>
        /// Desc:账号
        /// Default:
        /// Nullable:True
        /// </summary>           
        public string Account { get; set; }

        /// <summary>
        /// Desc:昵称
        /// Default:
        /// Nullable:True
        /// </summary>           
        public string Nickname { get; set; }

        /// <summary>
        /// Desc:学生编号
        /// Default:
        /// Nullable:True
        /// </summary>           
        public string StudentNumber { get; set; }

        /// <summary>
        /// Desc:姓名
        /// Default:
        /// Nullable:True
        /// </summary>           
        public string Name { get; set; }

        /// <summary>
        /// Desc:密码
        /// Default:
        /// Nullable:True
        /// </summary>           
        public string Password { get; set; }

        /// <summary>
        /// Desc:性别 男/女
        /// Default:
        /// Nullable:True
        /// </summary>           
        public string Sex { get; set; }

        /// <summary>
        /// Desc:邮箱
        /// Default:
        /// Nullable:True
        /// </summary>           
        public string Email { get; set; }

        /// <summary>
        /// Desc:评级 1-5
        /// Default:1
        /// Nullable:True
        /// </summary>           
        public int? Rating { get; set; }

        /// <summary>
        /// Desc:账号状态 冻结/正常
        /// Default:
        /// Nullable:True
        /// </summary>           
        public string State { get; set; }

        /// <summary>
        /// Desc:用户类型 学生/教师/助教
        /// Default:
        /// Nullable:True
        /// </summary>           
        public string Type { get; set; }

        /// <summary>
        /// Desc:注册时间
        /// Default:
        /// Nullable:True
        /// </summary>           
        public DateTime? RegisterTime { get; set; }

        /// <summary>
        /// Desc:登录IP
        /// Default:
        /// Nullable:True
        /// </summary>           
        public string LoginIp { get; set; }

        /// <summary>
        /// Desc:登录时间
        /// Default:
        /// Nullable:True
        /// </summary>           
        public DateTime? LoginTime { get; set; }

        /// <summary>
        /// Desc:头像
        /// Default:
        /// Nullable:True
        /// </summary>           
        public string Image { get; set; }

        /// <summary>
        /// Desc:介绍
        /// Default:
        /// Nullable:True
        /// </summary>           
        public string Introduce { get; set; }

        /// <summary>
        /// Desc:删除状态
        /// Default:
        /// Nullable:True
        /// </summary>           
        public bool? DeleteState { get; set; }

        /// <summary>
        /// Desc:修改人
        /// Default:
        /// Nullable:True
        /// </summary>           
        public long? UpdateUser { get; set; }

        /// <summary>
        /// Desc:修改时间
        /// Default:
        /// Nullable:True
        /// </summary>           
        public DateTime? UpdateTime { get; set; }

        /// <summary>
        /// Desc:微信用户唯一标识
        /// Default:
        /// Nullable:True
        /// </summary>           
        public string OpenId { get; set; }

        /// <summary>
        /// Desc:学校Id
        /// Default:
        /// Nullable:True
        /// </summary>           
        public long? SchoolId { get; set; }

        /// <summary>
        /// Desc:初中、高中、初完中、高完中 对应枚举
        /// Default:
        /// Nullable:True
        /// </summary>           
        public string SchoolType { get; set; }

        /// <summary>
        /// Desc:年级 ps:高2020级历史方向
        /// Default:
        /// Nullable:True
        /// </summary>           
        public string GraduationYear { get; set; }

        /// <summary>
        /// Desc:所属专业Ids
        /// Default:
        /// Nullable:True
        /// </summary>           
        public string SubjectIds { get; set; }

        /// <summary>
        /// Desc:所属专业名称
        /// Default:
        /// Nullable:True
        /// </summary>           
        public string SubjectName { get; set; }

        /// <summary>
        /// Desc:学校名称
        /// Default:
        /// Nullable:True
        /// </summary>           
        public string SchoolName { get; set; }

        /// <summary>
        /// Desc:云校学号
        /// Default:
        /// Nullable:True
        /// </summary>           
        public string OnlineSchoolNumber { get; set; }

        /// <summary>
        /// Desc:班级名称
        /// Default:
        /// Nullable:True
        /// </summary>           
        public string ClassName { get; set; }

        /// <summary>
        /// Desc:老师Id
        /// Default:
        /// Nullable:True
        /// </summary>           
        public long? TeacherId { get; set; }

    }
}
