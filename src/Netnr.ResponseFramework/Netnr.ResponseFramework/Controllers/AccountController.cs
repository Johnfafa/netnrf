﻿using System;
using System.ComponentModel;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Netnr.Data;
using Netnr.Domain;
using Netnr.Func.ViewModel;
using Netnr.Login;

namespace Netnr.ResponseFramework.Controllers
{
    /// <summary>
    /// 账号
    /// </summary>
    public class AccountController : Controller
    {
        #region 登录
        [Description("生成验证码")]
        public FileResult Captcha()
        {
            string num = Core.RandomTo.NumCode(4);
            byte[] bytes = Fast.ImageTo.CreateImg(num);
            HttpContext.Session.SetString("captcha", Core.CalcTo.MD5(num.ToLower()));
            return File(bytes, "image/jpeg");
        }

        [Description("登录页面")]
        public IActionResult Login()
        {
            return View();
        }

        /// <summary>
        /// 登录验证
        /// </summary>
        /// <param name="mo"></param>
        /// <param name="captcha">验证码</param>
        /// <param name="remember">1记住登录状态</param>
        /// <returns></returns>
        [Description("登录验证")]
        public async Task<AccountValidationVM> LoginValidation(SysUser mo, string captcha, int remember)
        {
            var result = new AccountValidationVM();

            var outMo = new SysUser();

            //跳过验证码
            if (captcha == "_pass_")
            {
                outMo = mo;
            }
            else
            {
                var capt = HttpContext.Session.GetString("captcha");

                if (string.IsNullOrWhiteSpace(captcha) || (capt ?? "") != Core.CalcTo.MD5(captcha.ToLower()))
                {
                    result.code = 104;
                    result.message = "验证码错误或已过期";
                    return result;
                }

                if (string.IsNullOrWhiteSpace(mo.SuName) || string.IsNullOrWhiteSpace(mo.SuPwd))
                {
                    result.code = 101;
                    result.message = "用户名或密码不能为空";
                    return result;
                }

                using var db = new ContextBase();
                outMo = db.SysUser.Where(x => x.SuName == mo.SuName && x.SuPwd == Core.CalcTo.MD5(mo.SuPwd, 32)).FirstOrDefault();
            }

            if (outMo == null || string.IsNullOrWhiteSpace(outMo.SuId))
            {
                result.code = 102;
                result.message = "用户名或密码错误";
                return result;
            }

            if (outMo.SuStatus != 1)
            {
                result.code = 103;
                result.message = "用户已被禁止登录";
                return result;
            }

            try
            {
                #region 授权访问信息

                //登录信息
                var identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme);
                identity.AddClaim(new Claim(ClaimTypes.PrimarySid, outMo.SuId));
                identity.AddClaim(new Claim(ClaimTypes.Name, outMo.SuName));
                identity.AddClaim(new Claim(ClaimTypes.GivenName, outMo.SuNickname ?? ""));
                identity.AddClaim(new Claim(ClaimTypes.Role, outMo.SrId));

                //配置
                var authParam = new AuthenticationProperties();
                if (remember == 1)
                {
                    authParam.IsPersistent = true;
                    authParam.ExpiresUtc = DateTime.Now.AddDays(10);
                }

                //写入
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity), authParam);

                result.code = 100;
                result.message = "登录成功";
                result.url = "/";
                return result;

                #endregion
            }
            catch (Exception ex)
            {
                result.code = 105;
                result.message = "处理登录请求出错（" + ex.Message + "）";
                return result;
            }
        }
        #endregion

        #region 注销
        [Description("注销")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            //清空全局缓存
            Func.Common.GlobalCacheRmove();

            return Redirect("/");
        }
        #endregion

        #region 修改密码
        [Description("修改密码页面")]
        [Authorize]
        public IActionResult UpdatePassword()
        {
            return View();
        }

        /// <summary>
        /// 修改为新的密码
        /// </summary>
        /// <param name="oldpwd">现有</param>
        /// <param name="newpwd1">新</param>
        /// <param name="newpwd2"></param>
        /// <returns></returns>
        [Description("执行修改密码")]
        [Authorize]
        public IActionResult UpdateNewPassword(string oldpwd, string newpwd1, string newpwd2)
        {
            string result = "fail";

            if (string.IsNullOrWhiteSpace(oldpwd) || string.IsNullOrWhiteSpace(newpwd1))
            {
                result = "密码不能为空";
            }
            else if (newpwd1.Length < 5)
            {
                result = "密码长度至少 5 位";
            }
            else if (newpwd1 != newpwd2)
            {
                result = "两次输入的密码不一致";
            }
            else
            {
                var userinfo = Func.Common.GetLoginUserInfo(HttpContext);

                using var db = new ContextBase();
                var mo = db.SysUser.Find(userinfo.UserId);
                if (mo != null && mo.SuPwd == Core.CalcTo.MD5(oldpwd))
                {
                    mo.SuPwd = Core.CalcTo.MD5(newpwd1);
                    db.SysUser.Update(mo);
                    db.SaveChanges();

                    result = "success";
                }
                else
                {
                    result = "现有密码错误";
                }
            }

            return Content(result);
        }
        #endregion
    }
}