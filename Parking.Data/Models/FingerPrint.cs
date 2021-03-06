﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace Parking.Data
{
    /// <summary>
    /// 指纹库，保存注册指纹
    /// 添加用户后，才允许绑定指纹，
    /// 一个用户可以绑定多个指纹
    /// </summary>
    public class FingerPrint
    {
        public int ID { get; set; }
        [Required]
        /// <summary>
        /// 编号，5位数，唯一
        /// int32范围（10000-22000）
        /// </summary>
        public int SN_Number { get; set; } 
        [Required]       
        /// <summary>
        /// 指纹信息
        /// </summary>
        public string FingerInfo { get; set; }        
        /// <summary>
        /// 顾客ID
        /// </summary>     
        public int CustID { get; set; }
    }
}
