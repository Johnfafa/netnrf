﻿using Microsoft.AspNetCore.Http;
using Netnr.Data;
using Netnr.Domain;
using Netnr.Func.ViewModel;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using System.Security.Claims;

namespace Netnr.Func
{
    /// <summary>
    /// 公共、常用方法
    /// </summary>
    public class Common
    {
        #region 字典  

        private static Dictionary<string, string> _dicSqlRelation;
        /// <summary>
        /// 数据库查询条件关系符
        /// </summary>
        public static Dictionary<string, string> DicSqlRelation
        {
            get
            {
                if (_dicSqlRelation == null)
                {
                    var ts = @"
                                Equal: '{0} = {1}',
                                NotEqual: '{0} != {1}',
                                LessThan: '{0} < {1}',
                                GreaterThan: '{0} > {1}',
                                LessThanOrEqual: '{0} <= {1}',
                                GreaterThanOrEqual: '{0} >= {1}',
                                BetweenAnd: '{0} >= {1} AND {0} <= {2}',
                                Contains: '%{0}%',
                                StartsWith: '{0}%',
                                EndsWith: '%{0}',
                                In: 'IN',
                                NotIn: 'NOT IN'
                              ".Split(',').ToList();
                    var dic = new Dictionary<string, string>();
                    foreach (var item in ts)
                    {
                        var ms = item.Split(':').ToList();
                        dic.Add(ms.FirstOrDefault().Trim(), ms.LastOrDefault().Trim().Replace("'", ""));
                    }
                    _dicSqlRelation = dic;
                }
                return _dicSqlRelation;
            }
            set
            {
                _dicSqlRelation = value;
            }
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 查询拼接
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="query"></param>
        /// <param name="ivm"></param>
        /// <param name="db"></param>
        /// <param name="ovm"></param>
        public static void QueryJoin<T>(IQueryable<T> query, QueryDataInputVM ivm, ContextBase db, ref QueryDataOutputVM ovm)
        {
            //条件
            query = QueryWhere(query, ivm);

            //总条数
            ovm.total = query.Count();

            //排序
            if (!string.IsNullOrWhiteSpace(ivm.sort))
            {
                query = Fast.QueryableTo.OrderBy(query, ivm.sort, ivm.order);
            }

            //分页
            if (ivm.pagination == 1)
            {
                query = query.Skip((Math.Max(ivm.page, 1) - 1) * ivm.rows).Take(ivm.rows);
            }

            //数据
            var data = query.ToList();
            ovm.data = data;
            //导出时，存储数据表格
            if (ivm.handleType == "export")
            {
                ovm.table = ToDataTableForString(data);
            }

            //列
            if (ivm.columnsExists != 1)
            {
                ovm.columns = db.SysTableConfig.Where(x => x.TableName == ivm.tableName).OrderBy(x => x.ColOrder).ToList();
            }
        }

        /// <summary>
        /// 查询条件（IQueryable）
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="query"></param>
        /// <param name="ivm"></param>
        /// <returns></returns>
        public static IQueryable<T> QueryWhere<T>(IQueryable<T> query, QueryDataInputVM ivm)
        {
            //条件
            if (!string.IsNullOrWhiteSpace(ivm.wheres))
            {
                var whereItems = JArray.Parse(ivm.wheres);
                foreach (var item in whereItems)
                {
                    //关系符
                    var relation = item["relation"].ToStringOrEmpty();
                    string rel = DicSqlRelation[relation];

                    //字段
                    var field = item["field"].ToStringOrEmpty();
                    //值
                    var value = item["value"];

                    //值引号
                    var vqm = "\"";

                    switch (relation)
                    {
                        case "Equal":
                        case "NotEqual":
                        case "LessThan":
                        case "GreaterThan":
                        case "LessThanOrEqual":
                        case "GreaterThanOrEqual":
                            {
                                string val = vqm + value.ToStringOrEmpty() + vqm;
                                string iwhere = string.Format(rel, field, val);
                                query = DynamicQueryableExtensions.Where(query, iwhere);
                            }
                            break;
                        case "Contains":
                        case "StartsWith":
                        case "EndsWith":
                            {
                                query = DynamicQueryableExtensions.Where(query, field + "." + relation + "(@0)", value.ToStringOrEmpty());
                            }
                            break;
                        case "BetweenAnd":
                            if (value.Count() == 2)
                            {
                                var v1 = vqm + value[0].ToString() + vqm;
                                var v2 = vqm + value[1].ToString() + vqm;

                                var iwhere = string.Format(rel, field, v1, v2);
                                query = DynamicQueryableExtensions.Where(query, iwhere);
                            }
                            break;
                    }
                }
            }
            return query;
        }

        /// <summary>
        /// 查询条件（IEnumerable,仅支持部分）
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="query"></param>
        /// <param name="ivm"></param>
        /// <returns></returns>
        public static IEnumerable<T> QueryWhere<T>(IEnumerable<T> query, QueryDataInputVM ivm)
        {
            //条件
            if (!string.IsNullOrWhiteSpace(ivm.wheres))
            {
                var whereItems = JArray.Parse(ivm.wheres);
                foreach (var item in whereItems)
                {
                    //关系符
                    var relation = item["relation"].ToStringOrEmpty();
                    string rel = DicSqlRelation[relation];

                    //字段
                    var field = item["field"].ToStringOrEmpty();
                    //值
                    var value = item["value"].ToString().ToLower();

                    switch (relation)
                    {
                        case "Equal":
                            query = query.Where(x => x.GetType().GetProperty(field).GetValue(x, null).ToString().ToLower() == value);
                            break;
                        case "NotEqual":
                            query = query.Where(x => x.GetType().GetProperty(field).GetValue(x, null).ToString().ToLower() != value);
                            break;
                        case "Contains":
                            query = query.Where(x => x.GetType().GetProperty(field).GetValue(x, null).ToString().ToLower().Contains(value));
                            break;
                        case "StartsWith":
                            query = query.Where(x => x.GetType().GetProperty(field).GetValue(x, null).ToString().ToLower().StartsWith(value));
                            break;
                        case "EndsWith":
                            query = query.Where(x => x.GetType().GetProperty(field).GetValue(x, null).ToString().ToLower().EndsWith(value));
                            break;
                    }
                }
            }
            return query;
        }

        /// <summary>
        /// 实体转表，类型为字符串
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <returns></returns>
        public static DataTable ToDataTableForString<T>(List<T> list)
        {
            Type elementType = typeof(T);
            var t = new DataTable();
            elementType.GetProperties().ToList().ForEach(propInfo => t.Columns.Add(propInfo.Name, typeof(string)));
            foreach (T item in list)
            {
                var row = t.NewRow();
                elementType.GetProperties().ToList().ForEach(propInfo => row[propInfo.Name] = propInfo.GetValue(item, null) ?? DBNull.Value);
                t.Rows.Add(row);
            }
            return t;
        }

        #endregion

        #region 获取登录用户信息

        /// <summary>
        /// 获取登录用户信息
        /// </summary>
        /// <returns></returns>
        public static LoginUserVM GetLoginUserInfo(HttpContext context)
        {
            var loginUser = new LoginUserVM
            {
                UserId = context.User.FindFirst(ClaimTypes.PrimarySid)?.Value,
                UserName = context.User.FindFirst(ClaimTypes.Name)?.Value,
                Nickname = context.User.FindFirst(ClaimTypes.GivenName)?.Value,
                RoleId = context.User.FindFirst(ClaimTypes.Role)?.Value
            };

            return loginUser;
        }

        /// <summary>
        /// 获取登录用户角色信息
        /// </summary>
        /// <param name="context"></param>
        public static SysRole LoginUserRoleInfo(HttpContext context)
        {
            var lui = GetLoginUserInfo(context);
            if (!string.IsNullOrWhiteSpace(lui.RoleId))
            {
                return QuerySysRoleEntity(x => x.SrId == lui.RoleId);
            }
            return null;
        }

        #endregion

        #region 全局缓存

        /// <summary>
        /// 全局缓存KEY
        /// </summary>
        public class GlobalCacheKey
        {
            /// <summary>
            /// 菜单缓存KEY
            /// </summary>
            public const string SysMenu = "GlobalSysMenu";

            /// <summary>
            /// 按钮缓存KEY
            /// </summary>
            public const string SysButton = "GlobalSysButton";
        }

        /// <summary>
        /// 清空全局缓存
        /// </summary>
        public static void GlobalCacheRmove()
        {
            Core.CacheTo.Remove(GlobalCacheKey.SysMenu);
            Core.CacheTo.Remove(GlobalCacheKey.SysButton);
        }

        #endregion

        #region 查询系统表

        /// <summary>
        /// 查询配置信息
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public static List<SysTableConfig> QuerySysTableConfigList(Expression<Func<SysTableConfig, bool>> predicate)
        {
            using var db = new ContextBase();
            var list = db.SysTableConfig.Where(predicate).OrderBy(x => x.ColOrder).ToList();
            return list;
        }

        /// <summary>
        /// 查询菜单
        /// </summary>
        /// <param name="predicate"></param>
        /// <param name="cache"></param>
        /// <returns></returns>
        public static List<SysMenu> QuerySysMenuList(Func<SysMenu, bool> predicate, bool cache = true)
        {
            if (!cache || !(Core.CacheTo.Get(GlobalCacheKey.SysMenu) is List<SysMenu> list))
            {
                using var db = new ContextBase();
                list = db.SysMenu.OrderBy(x => x.SmOrder).ToList();
                Core.CacheTo.Set(GlobalCacheKey.SysMenu, list, 300, false);
            }
            list = list.Where(predicate).ToList();
            return list;
        }

        /// <summary>
        /// 查询按钮
        /// </summary>
        /// <param name="predicate"></param>
        /// <param name="cache"></param>
        /// <returns></returns>
        public static List<SysButton> QuerySysButtonList(Func<SysButton, bool> predicate, bool cache = true)
        {
            if (!cache || !(Core.CacheTo.Get(GlobalCacheKey.SysButton) is List<SysButton> list))
            {
                using var db = new ContextBase();
                list = db.SysButton.OrderBy(x => x.SbBtnOrder).ToList();
                Core.CacheTo.Set(GlobalCacheKey.SysButton, list, 300, false);
            }
            list = list.Where(predicate).ToList();
            return list;
        }

        /// <summary>
        /// 查询角色信息
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public static SysRole QuerySysRoleEntity(Expression<Func<SysRole, bool>> predicate)
        {
            using var db = new ContextBase();
            var mo = db.SysRole.Where(predicate).FirstOrDefault();
            return mo;
        }

        #endregion

    }
}