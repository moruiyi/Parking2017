﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Parking.Core;
using Parking.Auxiliary;

namespace Parking.Application
{
    /// <summary>
    /// 后台服务入口
    /// </summary>
    public class EqpService
    {
        private Dictionary<int,Thread> dic_taskThread;
       
        private bool isStart;        

        private Dictionary<int, WorkFlow> dic_WorkFlows;
        private Log log;

        #region 系统所需变量
        private int plcCount;
        private int plcRefresh;

        private int iccdRefresh;
        #endregion
        public EqpService()
        {           
            isStart = false;
            log = LogFactory.GetLogger("EqpService");
                       
            Init();
        }

        /// <summary>
        /// 运行状态
        /// </summary>
        public bool RunState
        {
            get
            {
                if (isStart)
                {
                    foreach (KeyValuePair<int, Thread> pair in dic_taskThread)
                    {
                        Thread thrd = pair.Value;
                        if (thrd.ThreadState != ThreadState.Running &&
                            thrd.ThreadState != ThreadState.WaitSleepJoin)
                        {
                            log.Debug("warhouse- "+pair.Key+" ,thread state- "+thrd.ThreadState.ToString());
                            return false;
                        }
                    }
                }
                return isStart;
            }
        }

        private void Init()
        {
            try
            {
                string warehouse = XMLHelper.GetRootNodeValueByXpath("root", "PlcCount");
                plcCount = string.IsNullOrEmpty(warehouse) ? 0 : Convert.ToInt32(warehouse);

                string prefresh = XMLHelper.GetRootNodeValueByXpath("root", "PLCRefresh");
                plcRefresh = string.IsNullOrEmpty(prefresh) ? 0 : Convert.ToInt32(prefresh);

                string icfresh = XMLHelper.GetRootNodeValueByXpath("root", "ICcdRefresh");
                iccdRefresh = string.IsNullOrEmpty(icfresh) ? 0 : Convert.ToInt32(icfresh);

                dic_WorkFlows = new Dictionary<int, WorkFlow>();
                dic_taskThread = new Dictionary<int, Thread>();
            }
            catch (Exception ex)
            {
                log.Error(ex.ToString());
            }
           
        }

        public bool OnStart()
        {
            try
            {
                log.Info("后台服务尝试启动...");

                if (plcCount < 1)
                {
                    log.Error("PlcCount=" + plcCount + " 配置出错！");
                }
                isStart = true;
                for (int i = 1; i < plcCount + 1; i++)
                {
                    XmlNode node = XMLHelper.GetPlcNodeByTagName("//root//setting", i.ToString(), "PlcIPAddress");
                    if (node != null)
                    {
                        string ipadrs = node.InnerText;  //plc ip地址

                        WorkFlow controller = new WorkFlow(ipadrs, i);
                        dic_WorkFlows.Add(i, controller);
                        //添加 S7 connection_1 连接项
                        XmlNode xnode = XMLHelper.GetPlcNodeByTagName("//root//setting", i.ToString(), "ConnectItem");
                        if (xnode != null)
                        {
                            string items = xnode.InnerText.Trim();
                            string[] array_items = items.Split(';');
                            if (array_items != null && array_items.Length > 4)
                            {
                                controller.S7_Connection_Items = new string[array_items.Length];
                                int te = 0;
                                foreach (string item in array_items)
                                {
                                    controller.S7_Connection_Items[te++] = item.Trim();
                                }
                                log.Info("S7_Connection 连接项");
                                int ik = 1;
                                string msg = "";
                                foreach (string item in controller.S7_Connection_Items)
                                {
                                    msg += (ik++).ToString() + "、" + item + Environment.NewLine;
                                }
                                log.Info(msg);
                            }
                        }

                    }
                }
                for (int i = 1; i < plcCount + 1; i++)
                {                   
                    //启动作业线程
                    Thread taskThread = new Thread(new ParameterizedThreadStart(DealMessage));
                    taskThread.Start(i);
                    dic_taskThread.Add(i, taskThread);
                }
                
                log.Info("后台服务启动中...");
            }
            catch (Exception ex)
            {
                log.Error(ex.ToString());
            }
            return true;
        }

        public bool OnStop()
        {           
            try
            {
                log.Info("后台服务尝试停止...");

                isStart = false;
                foreach(KeyValuePair<int,WorkFlow> pair in dic_WorkFlows)
                {
                    WorkFlow controller = pair.Value;
                    controller.DisConnect();
                }

                foreach(KeyValuePair<int,Thread> pair in dic_taskThread)
                {
                    Thread mainThread = pair.Value;
                    mainThread.Abort();                
                }

                dic_WorkFlows.Clear();

                dic_taskThread.Clear();
            }
            catch (Exception ex)
            {
                log.Error(ex.ToString());
            }
            return true;
        }



        private void DealMessage(object wh)
        {          
            int warehouse = Convert.ToInt32(wh);
            if (!dic_WorkFlows.ContainsKey(warehouse))
            {
                log.Error("dic_WorkFlows没有包含key-"+warehouse+" 系统无法启动！");
                return;
            }
            WorkFlow controller = dic_WorkFlows[warehouse];
            try
            {
                controller.ConnectPLC();
            }
            catch (Exception ex)
            {
                log.Error("连接PLC异常，无法打开连接！系统无法启动！"+ex.ToString());               
                //return;
            }
            while (isStart)
            {
                try
                {
                    controller.DealAlarmInfo();
                    controller.TaskAssign();
                    controller.SendMessage();
                    controller.ReceiveMessage();                    
                    Thread.Sleep(plcRefresh);
                }
                catch(Exception ec)
                {
                    log.Error("处理业务异常-" + ec.ToString());
                    Thread.Sleep(15000);
                }               
            }


        }
    }
}
