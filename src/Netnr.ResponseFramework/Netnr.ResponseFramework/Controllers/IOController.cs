using System;
using System.Data;
using System.IO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Netnr.Data;
using Netnr.Func;

namespace Netnr.ResponseFramework.Controllers
{
    /// <summary>
    /// 输入输出
    /// </summary>
    [Authorize]
    [Route("[controller]/[action]")]
    public class IOController : Controller
    {
        public ContextBase db;
        public IOController(ContextBase cb)
        {
            db = cb;
        }

        #region 导出

        /// <summary>
        /// 公共导出
        /// </summary>
        /// <param name="ivm"></param>
        /// <param name="title">标题，文件名</param>
        /// <returns></returns>
        [HttpGet]
        public ActionResultVM Export(QueryDataInputVM ivm, string title = "export")
        {
            var vm = new ActionResultVM();

            //文件路径
            string path = "/upload/temp/";
            var vpath = GlobalTo.WebRootPath + path;

            if (!Directory.Exists(vpath))
            {
                Directory.CreateDirectory(vpath);
            }

            //文件名
            string filename = title.Replace(" ", "").Trim() + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".xlsx";

            //导出的表数据
            var dtReport = new DataTable();

            try
            {
                switch (ivm.tableName?.ToLower())
                {
                    default:
                        vm.Set(ARTag.invalid);
                        break;

                    //角色
                    case "sysrole":
                        {
                            using var ctl = new SettingController(db);
                            dtReport = ExportAid.ModelsMapping(ivm, ctl.QuerySysRole(ivm));
                        }
                        break;

                    //用户
                    case "sysuser":
                        {
                            using var ctl = new SettingController(db);
                            dtReport = ExportAid.ModelsMapping(ivm, ctl.QuerySysUser(ivm));
                        }
                        break;

                    //日志
                    case "syslog":
                        {
                            using var ctl = new SettingController(db);
                            dtReport = ExportAid.ModelsMapping(ivm, ctl.QuerySysLog(ivm));
                        }
                        break;

                    //字典
                    case "sysdictionary":
                        {
                            using var ctl = new SettingController(db);
                            dtReport = ExportAid.ModelsMapping(ivm, ctl.QuerySysDictionary(ivm));
                        }
                        break;
                }

                if (vm.msg != ARTag.invalid.ToString())
                {
                    //生成
                    if (Fast.NpoiTo.DataTableToExcel(dtReport, vpath + filename))
                    {
                        vm.data = path + filename;

                        //生成的Excel继续操作
                        ExportAid.ExcelDraw(vpath + filename, ivm);

                        vm.Set(ARTag.success);
                    }
                    else
                    {
                        vm.Set(ARTag.fail);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                vm.Set(ex);
            }

            return vm;
        }

        /// <summary>
        /// 导出下载
        /// </summary>
        /// <param name="path">下载文件路径</param>
        [HttpGet]
        public void ExportDown(string path)
        {
            path = GlobalTo.ContentRootPath + path;
            new Fast.DownTo(Response).Stream(path, "");
        }

        #endregion
    }
}