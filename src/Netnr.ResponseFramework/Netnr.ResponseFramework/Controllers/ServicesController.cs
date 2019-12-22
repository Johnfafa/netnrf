using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentScheduler;
using Microsoft.AspNetCore.Mvc;
using Netnr.Data;

namespace Netnr.ResponseFramework.Controllers
{
    /// <summary>
    /// 服务
    /// </summary>
    [Route("[controller]/[action]")]
    public class ServicesController : Controller
    {
        public ContextBase db;
        public ServicesController(ContextBase cb)
        {
            db = cb;
        }

        #region 定时任务

        //帮助文档：https://github.com/fluentscheduler/FluentScheduler

        /// <summary>
        /// 任务项
        /// </summary>
        public enum TaskItem
        {
            /// <summary>
            /// 重置数据库
            /// </summary>
            ResetDataBase,

            /// <summary>
            /// 清理临时目录
            /// </summary>
            ClearTemp
        }

        /// <summary>
        /// 执行任务项，支持名称或索引
        /// </summary>
        /// <returns></returns>
        [HttpGet("{id}")]
        public ActionResultVM ExecTask()
        {
            var vm = new ActionResultVM();

            Enum.TryParse(typeof(TaskItem), RouteData.Values["id"]?.ToString(), true, out object ti);

            switch (ti as TaskItem?)
            {
                default:
                    vm.Set(ARTag.invalid);
                    break;

                case TaskItem.ResetDataBase:
                    {
                        vm = new Func.DataMirrorAid().AddForJson();
                    }
                    break;

                case TaskItem.ClearTemp:
                    {
                        vm = Func.TaskAid.ClearTemp();
                    }
                    break;
            }

            return vm;
        }

        #endregion
    }
}