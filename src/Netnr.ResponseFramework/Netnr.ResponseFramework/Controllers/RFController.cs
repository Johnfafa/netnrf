﻿using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Netnr.Data;
using Netnr.Func.ViewModel;
using System;
using System.Collections.Generic;
using Netnr.Domain;

namespace Netnr.ResponseFramework.Controllers
{
    /// <summary>
    /// 示例，请删除
    /// </summary>
    public class RFController : Controller
    {
        #region 表配置示例

        /// <summary>
        /// 表配置示例页面
        /// </summary>
        /// <returns></returns>
        public IActionResult Tce()
        {
            return View();
        }

        /// <summary>
        /// 查询表配置示例
        /// </summary>
        /// <param name="ivm"></param>
        /// <returns></returns>
        public QueryDataOutputVM QueryTempExample(QueryDataInputVM ivm)
        {
            var ovm = new QueryDataOutputVM();
            using (var db = new ContextBase())
            {
                var query = db.TempExample;
                Func.Common.QueryJoin(query, ivm, db, ref ovm);
            }
            return ovm;
        }

        #endregion

        #region DataGrid示例页面

        /// <summary>
        /// DataGrid示例页面
        /// </summary>
        /// <returns></returns>
        public IActionResult DataGrid()
        {
            return View();
        }

        /// <summary>
        /// 查询表配置
        /// </summary>
        /// <param name="ivm"></param>
        /// <returns></returns>
        public QueryDataOutputVM QuerySysTableConfig(QueryDataInputVM ivm)
        {
            var ovm = new QueryDataOutputVM();
            using (var db = new ContextBase())
            {
                var query = db.SysTableConfig;
                Func.Common.QueryJoin(query, ivm, db, ref ovm);
            }
            return ovm;
        }

        #endregion

        #region TreeGrid示例页面

        /// <summary>
        /// TreeGrid示例页面
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public IActionResult TreeGrid(string id)
        {
            if (!string.IsNullOrWhiteSpace(id))
            {
                using var db = new ContextBase();
                var query = from a in db.SysMenu
                            where a.SmPid == id
                            orderby a.SmOrder
                            select new
                            {
                                a.SmId,
                                a.SmPid,
                                a.SmName,
                                a.SmUrl,
                                a.SmOrder,
                                a.SmIcon,
                                a.SmStatus,
                                a.SmGroup,
                                //查询是否有子集
                                state = (from b in db.SysMenu where b.SmPid == a.SmId select b.SmId).Count() > 0 ? "closed" : "open"
                            };
                var list = query.ToList();
                return Content(list.ToJson());
            }

            return View();
        }

        /// <summary>
        /// 查询系统菜单
        /// </summary>
        /// <param name="ivm"></param>
        /// <returns></returns>
        public QueryDataOutputVM QuerySysMenu(QueryDataInputVM ivm)
        {
            var ovm = new QueryDataOutputVM();
            using (var db = new ContextBase())
            {
                var query = db.SysMenu;
                Func.Common.QueryJoin(query, ivm, db, ref ovm);
            }
            return ovm;
        }

        #endregion

        #region Grid表格联动

        /// <summary>
        /// Grid表格联动
        /// </summary>
        /// <returns></returns>
        public IActionResult GridChange()
        {
            return View();
        }

        /// <summary>
        /// Grid多表格-主表
        /// </summary>
        /// <param name="ivm"></param>
        /// <returns></returns>
        public QueryDataOutputVM QueryGridChange1(QueryDataInputVM ivm)
        {
            var ovm = new QueryDataOutputVM();
            using (var db = new ContextBase())
            {
                var query = db.SysRole;
                Func.Common.QueryJoin(query, ivm, db, ref ovm);
            }
            return ovm;
        }

        /// <summary>
        /// Grid多表格-子表
        /// </summary>
        /// <param name="ivm"></param>
        /// <returns></returns>
        public QueryDataOutputVM QueryGridChange2(QueryDataInputVM ivm)
        {
            var ovm = new QueryDataOutputVM();
            using (var db = new ContextBase())
            {
                var query = db.SysUser;
                Func.Common.QueryJoin(query, ivm, db, ref ovm);
            }
            return ovm;
        }

        #endregion

        #region 静态表单示例页面

        /// <summary>
        /// 静态表单示例页面
        /// </summary>
        /// <returns></returns>
        public IActionResult Form()
        {
            return View();
        }

        #endregion

        #region 生成多表单

        /// <summary>
        /// 生成多表单
        /// </summary>
        /// <returns></returns>
        public IActionResult BuildForms()
        {
            return View();
        }

        #endregion

        #region 单据

        /// <summary>
        /// 单据
        /// </summary>
        /// <returns></returns>
        public IActionResult Invoice()
        {
            return View();
        }

        /// <summary>
        /// 查询单据主表
        /// </summary>
        /// <param name="ivm"></param>
        /// <returns></returns>
        public QueryDataOutputVM QueryInvoiceMain(QueryDataInputVM ivm)
        {
            var ovm = new QueryDataOutputVM();
            using (var db = new ContextBase())
            {
                var query = db.TempInvoiceMain;
                Func.Common.QueryJoin(query, ivm, db, ref ovm);
            }
            return ovm;
        }

        /// <summary>
        /// 查询单据明细表
        /// </summary>
        /// <param name="ivm"></param>
        /// <returns></returns>
        public QueryDataOutputVM QueryInvoiceDetail(QueryDataInputVM ivm)
        {
            var ovm = new QueryDataOutputVM();
            using (var db = new ContextBase())
            {
                var query = from a in db.TempInvoiceDetail select a;
                if (string.IsNullOrWhiteSpace(ivm.pe1))
                {
                    query = query.Where(x => false);
                }
                else
                {
                    query = query.Where(x => x.TimId == ivm.pe1);
                }

                Func.Common.QueryJoin(query, ivm, db, ref ovm);
            }
            return ovm;
        }

        /// <summary>
        /// 保存单据
        /// </summary>
        /// <param name="moMain"></param>
        /// <param name="rows"></param>
        /// <returns></returns>
        public ActionResultVM SaveInvoiceForm(TempInvoiceMain moMain, string rows)
        {
            var vm = new ActionResultVM();

            //明细反序列化为对象
            var listDetail = rows.ToEntitys<TempInvoiceDetail>();

            //新增，补齐主表信息
            var isadd = string.IsNullOrWhiteSpace(moMain.TimId);
            if (isadd)
            {
                moMain.TimId = Guid.NewGuid().ToString();
                moMain.TimCreateTime = DateTime.Now;

                moMain.TimOwnerId = Guid.Empty.ToString();
                moMain.TimOwnerName = "系统登录人员";
            }

            using (var db = new ContextBase())
            {
                if (isadd)
                {
                    db.TempInvoiceMain.Add(moMain);
                }
                else
                {
                    db.TempInvoiceMain.Update(moMain);

                    //更新时，删除原有明细
                    var currDetail = db.TempInvoiceDetail.Where(x => x.TimId == moMain.TimId).ToList();
                    if (currDetail.Count > 0)
                    {
                        db.TempInvoiceDetail.RemoveRange(currDetail);
                    }
                }

                //添加明细
                if (listDetail.Count > 0)
                {
                    //初始值
                    foreach (var item in listDetail)
                    {
                        item.TidId = Guid.NewGuid().ToString();
                        item.TimId = moMain.TimId;
                    }

                    db.TempInvoiceDetail.AddRange(listDetail);
                }

                int num = db.SaveChanges();

                vm.Set(num > 0);

                if (isadd)
                {
                    vm.data = moMain.TimId;
                }
            }

            return vm;
        }

        #endregion

        #region 上传接口示例

        /// <summary>
        /// 公共上传示例
        /// </summary>
        /// <returns></returns>
        public IActionResult Upload()
        {
            return View();
        }

        #endregion

        #region 富文本

        /// <summary>
        /// 嵌入富文本
        /// </summary>
        /// <returns></returns>
        public IActionResult RichText()
        {
            return View();
        }

        #endregion

        #region Bulk Test，请手动修改 private 为 public 后测试

        /// <summary>
        /// 批量新增
        /// </summary>
        /// <returns></returns>
        private ActionResultVM BulkInsert()
        {
            var vm = new ActionResultVM();

            var list = new List<SysLog>();
            for (int i = 0; i < 50_000; i++)
            {
                var mo = new SysLog()
                {
                    LogId = Guid.NewGuid().ToString(),
                    LogAction = "/",
                    LogBrowserName = "Chrome",
                    LogCity = "重庆",
                    LogContent = "测试信息",
                    LogCreateTime = vm.startTime,
                    LogGroup = 1,
                    LogIp = "0.0.0.0",
                    LogSystemName = "Win10",
                    LogUrl = Request.Path,
                    SuName = "netnr",
                    SuNickname = "netnr",
                    LogRemark = "无"
                };
                list.Add(mo);
            }

            using (var db = new ContextBase())
            {
                db.SysLog.BulkInsert(list);

                db.BulkSaveChanges();

                vm.Set(ARTag.success);
            }

            return vm;
        }

        /// <summary>
        /// 批量修改
        /// </summary>
        /// <returns></returns>
        private ActionResultVM BulkUpdate()
        {
            var vm = new ActionResultVM();

            using (var db = new ContextBase())
            {
                var list = db.SysLog.OrderBy(x => x.LogCreateTime).Take(50_000).ToList();

                foreach (var item in list)
                {
                    item.LogRemark = Guid.NewGuid().ToString();
                }

                db.SysLog.BulkUpdate(list);

                db.BulkSaveChanges();

                vm.Set(ARTag.success);
            }

            return vm;
        }

        /// <summary>
        /// 批量删除
        /// </summary>
        /// <returns></returns>
        private ActionResultVM BulkDelete()
        {
            var vm = new ActionResultVM();

            using (var db = new ContextBase())
            {
                var list = db.SysLog.OrderBy(x => x.LogCreateTime).Take(50_000).ToList();

                db.SysLog.BulkDelete(list);

                db.BulkSaveChanges();

                vm.Set(ARTag.success);
            }

            return vm;
        }

        #endregion
    }
}