using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Web;
using System.Web.Mvc;
using Portal.Web.Models;
using Portal.Web.Controllers.Managers;
using Portal.Web.Helpers;
using Newtonsoft.Json;
using Portal.Web.Models.DataObject;
using Portal.Web.Repository;
using Portal.Web.Services;
using Portal.Web.Services.Filters;

namespace Portal.Web.Controllers
{
    [Authorize]
    public class CourseController : ApplicationController
    {
        [Authorize(Roles = "User")]
        public ActionResult Index()
        {
            if (Request.Url != null && Session[CustomizeHelper.sessionMarcker] == null)
                CustomizeHelper.SetSession(Session, CompanyService.CompanyByDomain(Request.Url.Host.ToLower()), null);
            Session["isCreated"] = false;

            var profile = SessionManager.inst.User();
            if (profile != null)
            {
                if (!profile.TourShowed)
                {
                    Session["isCreated"] = true;
                    SessionManager.inst.SetVisited();
                }
            }
            ViewBag.courses = CourseService.MasterCoursesAssignedTo(SessionManager.inst.User().Id, Services.Filters.FilterIdType.UserId, false);
            ViewBag.midlle = "_midlle.png";
            return View();
        }

        [Authorize(Roles = "User")]
        public ActionResult Course(int id)
        {
            ViewBag.modules = CourseService.ModulesAvaliableByCourseId(id);
            ViewBag.CourseId = id;
            ViewBag.course = CourseService.MasterCourse(id);
            return View();
        }
        public JsonResult ChangeCourseStatus(int courseID, int companyId, int departmentId, int userId, bool status)
        {
            bool rez = false;
            if (userId != 0)
                rez = CourseService.ChangeCourseStatusFor(userId, FilterIdType.UserId, courseID, status);
            else if (departmentId != 0)
                rez = CourseService.ChangeCourseStatusFor(departmentId, FilterIdType.DepartmentId, courseID, status);
            else if (companyId != 0)
                rez = CourseService.ChangeCourseStatusFor(companyId, FilterIdType.CompanyId, courseID, status);
            return Json(rez, JsonRequestBehavior.AllowGet);
        }
        public JsonResult ChangeOrder(string order)
        {
            List<CoursesOrder> lst = new List<CoursesOrder>();
            dynamic result = JsonConvert.DeserializeObject(order);
            foreach (var item in result)
            {
                lst.Add(new CoursesOrder() { id = item.id, orderNum = item.orderNum });
            }
            CourseService.ChangeMasterCourseOrder(lst);
            
            return Json(true, JsonRequestBehavior.AllowGet);
        }
        public JsonResult ChangeCustomOrder(int companyId, int departmentId, int userId, string order)
        {
            bool isSuperadmin = SessionManager.inst.IsSuperadmin();
            if (!isSuperadmin) return Json(false, JsonRequestBehavior.AllowGet);
            try
            {
                string result = order.Replace("[", "");
                result = result.Replace("]", "");
                var values = result.Split(',');
                int[] intValues = Array.ConvertAll(values, s => int.Parse(s));
                CourseRepository.CustomCoursesOrder(companyId, departmentId, userId, intValues);
                return Json(true, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(false, JsonRequestBehavior.AllowGet);
            }
        }
	}


}