using System;
using System.Collections.Generic;
using System.Text;

namespace SC_LaborReporting.SystemConfigs
{
    public class SystemConfigDto
    {
        public int AttendanceStartDate { get; set; }
        public int AttendanceEndDate { get; set; }
        public bool AuditStatus { get; set; }

        /// <summary>
        /// 企业微信CorpID设置
        /// </summary>
        public  string WeComCorpID { get; set; }

        /// <summary>
        /// 企业微信AgentId设置
        /// </summary>
        public  string WeComAgentId { get; set; }


        /// <summary>
        /// 企业微信Secret设置
        /// </summary>
        public  string WeComSecret { get; set; }
    }
}
