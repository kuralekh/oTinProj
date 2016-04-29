using System;
using System.Data;
using System.Globalization;
using Portal.Web.Models;
using System.Linq;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using System.Collections.Generic;
using Portal.Web.Models.DataObject;
using Portal.Web.Models.JobScheduler;
using Portal.Web.Controllers.Managers;
using System.Text.RegularExpressions;
using Portal.Web.Controllers.ViewModel;
using System.Security.Principal;
using System.Diagnostics;
using Portal.Web.Repository;
using Portal.Web.Helpers;
using Portal.Web.Services;
using Portal.Web.Services.Utilities;
using Portal.Web.Services.Filters;
using NewDB;

namespace Portal.Web.Controllers
{
    public class FilterData
    {
        public List<CoursesDto> courses = new List<CoursesDto>();
        public List<CoursesDto> users = new List<CoursesDto>();
        public List<CoursesDto> departments = new List<CoursesDto>();
    }
    public class AjaxController : ApplicationController
    {
        protected const string dateFormat = "dd.MM.yyyy";

        public ActionResult _ajax_GetCourseModuleAverageScore(int id, string start, string end)
        {
            int companyId = SessionManager.inst.Company().Id;
            DateTime? startDateTime = null;
            DateTime? endDateTime = null;
            if (!start.Equals("")) startDateTime = DateTime.ParseExact(start, dateFormat, CultureInfo.InvariantCulture);
            if (!end.Equals("")) endDateTime = DateTime.ParseExact(end, dateFormat, CultureInfo.InvariantCulture);

            var modules = AjaxRepository.GetModulesAverageScore(id, companyId, startDateTime, endDateTime);

           
            return Json(modules, JsonRequestBehavior.AllowGet);
        }


        public ActionResult _ajax_SetCurrentModuleId(int courseID, int moduleID)
        {
            var user = SessionManager.inst.User();
            CourseService.SetModuleSeen(user.Id, courseID, moduleID);
            return Json(ExamService.CheckIsCourseFinish(courseID, user.Id), JsonRequestBehavior.AllowGet);
        }

        public ActionResult getWatchedModules(int courseID)
        {
            var user = SessionManager.inst.User();
            List<int> watchedIDs = user.UserModulesWatched.Where(i => i.CourseID == courseID).Select(module => module.ModuleId).ToList();
            return Json(watchedIDs, JsonRequestBehavior.AllowGet);
        }
        public ActionResult _ajax_GetCourseUsersPassed(int courseid, int company, int department, string start, string end, int user)
        {
            var username = System.Security.Principal.WindowsPrincipal.Current.Identity.Name;
            var profile = UserService.GetUserProfilesBy(username, FilterUserProfiles.Username).FirstOrDefault();
            var userRole = UserService.IdentifyUserRoleByUsername(username);
            if (userRole == RolesNames.user)
            {
                user = profile.Id;
            }
            else if (userRole == RolesNames.manager)
            {
                department = profile.DepartmentId;
            }
            else if (userRole == RolesNames.administrator)
            {
                company = profile.CompanyId;
            }
            
            DateTime? startDateTime = null;
            DateTime? endDateTime = null;
            if (!start.Equals("")) startDateTime = DateTime.ParseExact(start, dateFormat, CultureInfo.InvariantCulture);
            if (!end.Equals("")) endDateTime = DateTime.ParseExact(end, dateFormat, CultureInfo.InvariantCulture);

            var result = StatisticsService.GetGraphTimeLineData(user, company,department, courseid, startDateTime, endDateTime);

            return Json(result, JsonRequestBehavior.AllowGet);
        }

        public ActionResult _ajax_GetPieStatistics(int course, int company, int department, string start, string end, int user)
        {
            bool superadmin = SessionManager.inst.IsSuperadmin();
            bool admin = SessionManager.inst.IsAdmin();
            bool manager = SessionManager.inst.IsManager();
            if (!superadmin)
                company = company != 0 ? company : SessionManager.inst.Company().Id;
            if (manager && !admin)
                department = department != 0 ? department : SessionManager.inst.User().DepartmentId;
            if (!superadmin && !admin && !manager)
            {
                var username = System.Security.Principal.WindowsPrincipal.Current.Identity.Name;
                var profile = AdminUserRepository.GetUserProfileByUsername(username);
                if (UserRepository.GetListOfRoles(username).Count() == 1 && UserRepository.GetListOfRoles(username).FirstOrDefault().Name == "User")
                {
                    user = profile.UserProfileId;
                }
            }

            var customCourseIds = StatisticsRepository.MasterId2customId(course, company, department, user).Distinct().ToList();
            DateTime? startDateTime = null;
            DateTime? endDateTime = null;
            if (start != null) startDateTime = DateTime.ParseExact(start, dateFormat, CultureInfo.InvariantCulture);
            if (end != null) endDateTime = DateTime.ParseExact(end, dateFormat, CultureInfo.InvariantCulture);


            var allAssignedCourses = StatisticsRepository.CoursesAssignedToUsers(company, department, user)
                .Where(c => course == 0 ? true : c.CustomCours.MasterCourseId == course).Select(i => i.CustomCourseId).ToList();

            var startedCourses = StatisticsRepository.StartedCourses(customCourseIds, company, department, user);

            var allCompletedCourses = StatisticsRepository.TotalCompletedCourses(company, department, startDateTime, endDateTime, user)
                            .Where(i => (course == 0 || i.CustomCours.MasterCourseId == course)).ToList();

            int coursesSartedNotCompleted = startedCourses - allCompletedCourses.Count();
            int coursesNotStarted = allAssignedCourses.Count() - startedCourses;
            decimal onePercent = 0;

            if (allAssignedCourses.Count() > 0)
            {
                onePercent = ((decimal)100 / allAssignedCourses.Count());
            }
            var obj = new GetPieStatisticsJson
            {
                usersCompleted = onePercent * allCompletedCourses.Count(),
                usersNotCompleted = onePercent * coursesSartedNotCompleted,
                usersNotStarted = onePercent * coursesNotStarted
            };
            //var result = _statisticsRepository.UsersCompletedCoursesCount(companyId, start, end);
            var json = new JavaScriptSerializer().Serialize(obj);
            return Json(json, JsonRequestBehavior.AllowGet);
        }

        public ActionResult OtherСompanys(int course)
        {
            var companys = SharedRepository.GetOtherAdminList(course);
            var json = new JavaScriptSerializer().Serialize(companys);
            return Json(json, JsonRequestBehavior.AllowGet);
        }

        public ActionResult _ajax_StatUsers(int course, int company, int department)
        {
            var customCourseIds = StatisticsRepository.MasterId2customId(course, company, department, 0).Distinct().ToList();
            List<StatUsersModel> stats = new List<StatUsersModel>();
            for (var i = 0; i < customCourseIds.Count(); i++)
                stats.AddRange(StatisticsRepository.GetStatUsers(course, customCourseIds[i], company, department));
            var json = new JavaScriptSerializer().Serialize(stats.Distinct());
            return Json(json, JsonRequestBehavior.AllowGet);
        }

        public ActionResult ListСompanies(int course, int company, int department, int user)
        {
            var companys = StatisticsRepository.DropDownLists(course, company, department, user);
            var json = new JavaScriptSerializer().Serialize(companys);
            return Json(json, JsonRequestBehavior.AllowGet);
        }

        //refactored
        //todo test it when the sessionManager will use newDB
        public ActionResult CoursesStats(int company, int department, int user)
        {
            var profile = SessionManager.inst.User();
            var userRole = UserService.IdentifyUserRoleByUsername(profile.AspNetUsers.UserName);
            if (userRole == RolesNames.user)
            {
                user = profile.Id;
            }
            else if (userRole == RolesNames.manager)
            {
                department = profile.DepartmentId;
            }
            else if (userRole == RolesNames.administrator)
            {
                company = profile.CompanyId;
            }
            /////
            var courses = new List<CoursesStatDto>();
            if (user != 0)
                courses = CourseService.GetCoursesForTableStat(user, Services.Filters.FilterIdType.UserId);
            else if (department != 0)
                courses = CourseService.GetCoursesForTableStat(department, Services.Filters.FilterIdType.DepartmentId);
            else if (company != 0)
                courses = CourseService.GetCoursesForTableStat(company, Services.Filters.FilterIdType.CompanyId);
            else
                courses = CourseService.GetCoursesForTableStat(0, Services.Filters.FilterIdType.All);
            /////
            var json = new JavaScriptSerializer().Serialize(courses);

            return Json(json, JsonRequestBehavior.AllowGet);
        }

        public ActionResult Shared(int id, int shared, string department, int course)
        {
            if (course == 0) SharedRepository.SharedDepartment(id, department, shared);
            else SharedRepository.SharedCourse(id, course, shared);
            return Json("ok", JsonRequestBehavior.AllowGet);
        }



        public ActionResult SetNewPassword(string email, string secretKey, string password)
        {
            try
            {
                UserService.ChangePassword(email, secretKey, password);
                return Json("Password changed. Now you will be redirected.", JsonRequestBehavior.AllowGet);
            }
            catch
            {
                return Json("Something went wrong", JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult _ajax_GetUserModuleStats(int course, string id, int company, int department, int user)
        {
            id = id ?? "";
            bool superadmin = SessionManager.inst.IsSuperadmin();
            bool admin = SessionManager.inst.IsAdmin();
            bool manager = SessionManager.inst.IsManager();
            if (!superadmin)
                company = company != 0 ? company : SessionManager.inst.Company().Id;
            if (manager && !admin)
                department = department != 0 ? department : SessionManager.inst.User().DepartmentId;
            //var graph = StatisticsRepository.GetUserScoreForCourse(id, course, company, department, user);
            var graph = StatisticsRepository.GetUsersScoreForCourse(course, id, company, department, user);
            if (graph != null)
                return Json(graph, JsonRequestBehavior.AllowGet);
            else
                return Json("no_data", JsonRequestBehavior.AllowGet); ;
        }
        public ActionResult deleteDepartment(int department)
        {
            //AdminUserRepository.DeleteDepartment(department);
            CompanyService.DeleteDepartment(department);
            return Json("true", JsonRequestBehavior.AllowGet);
        }

        public ActionResult FilterByDepartment(int departmentId)
        {
            var data = new FilterData();
            var cours = CourseRepository.GetDepartmentCourses(departmentId);
            var users = UserRepository.GetDeparmentUsers(departmentId);

            foreach (var item in cours)
                data.courses.Add(new CoursesDto
                {
                    CourseId = item.MasterCourseId,
                    CoverUrl = item.CoverUrl,
                    Name = item.Name,
                    ShortDescription = item.ShortDescription,
                    AuthorId = item.AuthorId.Value,
                    PublisherId = item.PublisherId != null ? item.PublisherId.Value : 0,
                    Time = item.CourseTime ?? 0,
                    Price = item.Price ?? 0,
                    Released = item.Released,
                    Exam = item.Exam == 1? true : false,
                    Exercises = item.Exercises == 1 ? true : false,
                    Diploma = item.Diploma == 1 ? true : false,
                    IntroUrl = item.IntroVideoUrl
                });

            foreach (var item in users)
                data.users.Add(new CoursesDto { Username = item.username, UserId = item.userId });

            return Json(data, JsonRequestBehavior.AllowGet);
        }
        public ActionResult FilterByCompany(int companyId)
        {
            var data = new FilterData();
            var cours = CourseRepository.GetCompanyCourses(companyId);
            var departments = CompanyRepository.GetDepartmentsByCompany(companyId);
            var users = UserRepository.GetCompanyUsers(companyId);

            foreach (var item in cours)
            {
                var auth_user = item.AuthorId != null ? UserRepository.GetUserProfileById(item.AuthorId.Value) : null;
                data.courses.Add(new CoursesDto
                {
                    CourseId = item.MasterCourseId,
                    CoverUrl = item.CoverUrl,
                    Name = item.Name,
                    ShortDescription = item.ShortDescription,
                    AuthorName = item.AuthorId == null ? "" : auth_user.Firstname + " " + auth_user.Lastname,
                    PublisherName = item.PublisherId == null ? "0" : CompanyRepository.GetCompanyName(item.PublisherId.Value),
                    Time = item.CourseTime ?? 0,
                    Price = item.Price ?? 0,
                    Released = item.Released,
                    Exam = item.Exam == 1 ? true : false,
                    Exercises = item.Exercises == 1 ? true : false,
                    Diploma = item.Diploma == 1 ? true : false,
                    IntroUrl = item.IntroVideoUrl
                });
            }

            foreach (var item in users)
                data.users.Add(new CoursesDto { Username = item.username, UserId = item.userId });

            foreach (var item in departments)
                data.departments.Add(new CoursesDto { DeparmentId = item.DepartmentId, DeparmentName = item.DepartmentName });

            return Json(data, JsonRequestBehavior.AllowGet);
        }

        public ActionResult _ajax_GetSparklineStat(int course, int company, int department, string start, string end)
        {
            bool superadmin = SessionManager.inst.IsSuperadmin();
            bool admin = SessionManager.inst.IsAdmin();
            bool manager = SessionManager.inst.IsManager();
            if (!superadmin)
                company = company != 0 ? company : SessionManager.inst.Company().Id;
            if (manager && !admin)
                department = department != 0 ? department : SessionManager.inst.User().DepartmentId;
            var customCourseId = StatisticsRepository.MasterId2customId(course, company, department, 0).FirstOrDefault();
            DateTime? startDateTime = null;
            DateTime? endDateTime = null;
            var result = new Dictionary<string, int>();
            if (!start.Equals("")) startDateTime = DateTime.ParseExact(start, dateFormat, CultureInfo.InvariantCulture);
            if (!end.Equals("")) endDateTime = DateTime.ParseExact(end, dateFormat, CultureInfo.InvariantCulture);
            var started = 0;
            var ended = 0;
            if (customCourseId != 0)
            {
                var customCourseIds = StatisticsRepository.masterId2customIds(course);
                for (var i = 0; i < customCourseIds.Count(); i++)
                {
                    started += StatisticsRepository.CourseWeekStatistics(customCourseIds[i], company, department, startDateTime, endDateTime, 0);
                    ended += StatisticsRepository.CoursesWeekStatistics(customCourseIds[i], company, department, startDateTime, endDateTime, 0);
                }
            }
            else
            {
                started += StatisticsRepository.CourseWeekStatistics(customCourseId, company, department, startDateTime, endDateTime, 0);
                ended += StatisticsRepository.CoursesWeekStatistics(customCourseId, company, department, startDateTime, endDateTime, 0);
            }
            result.Add("started", started);
            result.Add("ended", ended);
            var json = new JavaScriptSerializer().Serialize(result);
            return Json(json, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Invokes for libraryView to set avaliable and assigned courses, list of departments and users
        /// </summary>
        /// <param name="companyId">id of selected company, 0 - if not used</param>
        /// <param name="departmentId">id of selected department, 0 if not used</param>
        /// <param name="userId">id of selected user, 0 if not used</param>
        /// <returns></returns>
        public JsonResult FilterCourses(int companyId, int departmentId, int userId)
        {
            LibraryFilter filter = new LibraryFilter();
            //getting avaliable courses
            var listavaliable = new List<MasterCourses>();
            //getting assigned courses
            var listassigned = new List<MasterCourses>();

            if (userId != 0)
            {
                listavaliable = CourseService.MasterCoursesAvaliableTo(userId, FilterIdType.UserId);
                listassigned = CourseService.MasterCoursesAssignedTo(userId, FilterIdType.UserId);
                
                filter.assigned = listassigned.Select(item => new CoursesDto
                {
                    CourseId = item.Id,
                    CoverUrl = item.CoverUrl,
                    Name = item.Name,
                    ShortDescription = item.ShortDescription,
                    Time = item.CourseTime ?? 0,
                    Price = item.Price ?? 0,
                    Released = item.Released,
                    Exam = item.Exam,
                    Exercises = item.Exercises,
                    Diploma = item.Diploma,
                    TrailerVideoUrl = item.TrailerVideoUrl,
                    CourseStatus = CourseService.GetCourseStatus(userId, FilterIdType.UserId, item.Id),
                    CategoryId = item.CourseCategoryId,
                    CreatorId = item.CreatorId,
                    OrderNum = item.OrderNum
                }).OrderBy(i => i.OrderNum).ToList();
                filter.filterBy = "user";
            }
            else if (departmentId != 0)
            {
                listavaliable = CourseService.MasterCoursesAvaliableTo(departmentId, FilterIdType.DepartmentId);
                listassigned = CourseService.MasterCoursesAssignedTo(departmentId, FilterIdType.DepartmentId);

                filter.assigned = listassigned.Select(item => new CoursesDto
                {
                    CourseId = item.Id,
                    CoverUrl = item.CoverUrl,
                    Name = item.Name,
                    ShortDescription = item.ShortDescription,
                    Time = item.CourseTime ?? 0,
                    Price = item.Price ?? 0,
                    Released = item.Released,
                    Exam = item.Exam,
                    Exercises = item.Exercises,
                    Diploma = item.Diploma,
                    TrailerVideoUrl = item.TrailerVideoUrl,
                    CourseStatus = CourseService.GetCourseStatus(departmentId, FilterIdType.DepartmentId, item.Id),
                    CategoryId = item.CourseCategoryId,
                    CreatorId = item.CreatorId,
                    OrderNum = item.OrderNum
                }).OrderBy(i => i.OrderNum).ToList();

                var dep = CompanyService.GetDepartment(departmentId);
                filter.users = dep.UserProfiles.Select(i => new UserSimple { name = i.Firstname+" "+i.Lastname, id = i.Id }).ToList();
                filter.filterBy = "department";
            }
            else if (companyId != 0)
            {
                listavaliable = CourseService.MasterCoursesAvaliableTo(companyId, FilterIdType.CompanyId);
                listassigned = CourseService.MasterCoursesAssignedTo(companyId, FilterIdType.CompanyId);
                filter.assigned = listassigned.Select(item => new CoursesDto
                {
                    CourseId = item.Id,
                    CoverUrl = item.CoverUrl,
                    Name = item.Name,
                    ShortDescription = item.ShortDescription,
                    Time = item.CourseTime ?? 0,
                    Price = item.Price ?? 0,
                    Released = item.Released,
                    Exam = item.Exam,
                    Exercises = item.Exercises,
                    Diploma = item.Diploma,
                    TrailerVideoUrl = item.TrailerVideoUrl,
                    CourseStatus = CourseService.GetCourseStatus(companyId, FilterIdType.CompanyId, item.Id),
                    CategoryId = item.CourseCategoryId,
                    CreatorId = item.CreatorId,
                    OrderNum = item.OrderNum
                }).OrderBy(i => i.OrderNum).ToList();
                
                var company = CompanyService.CompanyById(companyId);
                filter.departments = company.Departments.Select(i => new DepartmentSimple { id = i.Id, name = i.DepartmentName }).ToList();
                filter.users = company.UserProfiles.Select(i => new UserSimple { name = i.Firstname+" "+i.Lastname, id = i.Id }).ToList();
                filter.filterBy = "company";
            }
            else
                listavaliable = CourseService.GetAllMasterCourses();

            filter.aviable = listavaliable.Select(item => new CoursesDto
            {
                CourseId = item.Id,
                CoverUrl = item.CoverUrl,
                Name = item.Name,
                ShortDescription = item.ShortDescription,
                Time = item.CourseTime ?? 0,
                Price = item.Price ?? 0,
                Released = item.Released,
                Exam = item.Exam,
                Exercises = item.Exercises,
                Diploma = item.Diploma,
                TrailerVideoUrl = item.TrailerVideoUrl,
                CourseStatus = item.CourseStatus,
                CategoryId = item.CourseCategoryId,
                CreatorId = item.CreatorId,
                OrderNum = item.OrderNum
            }).OrderBy(i => i.OrderNum).ToList();

            return Json(filter, JsonRequestBehavior.AllowGet);
        }
        public JsonResult LibraryViewUser()
        {
            LibraryFilter filter = new LibraryFilter();
            var user = SessionManager.inst.User();

            List<MasterCours> aviableCourse = new List<MasterCours>();
            switch (user.DragnDrop)
            {
                //None
                case 0:
                    break;
                //Company
                case 1:
                    //finish this
                    //aviableCourse = CourseRepository.AvaibleCompanyUserCourses(user);
                    break;
                //Library
                case 2:
                    aviableCourse = CourseRepository.GetAvaibleCourses(0, 0, user.Id, user.CompanyId);
                    break;
                //All
                case 3:
                    aviableCourse = CourseRepository.GetAvaibleCourses(0, 0, user.Id, user.CompanyId);
                    break;
            }

            filter.assigned = CourseRepository.UsersCourseJsonDto(user.Id).Where(item => item.CourseStatus == true).ToList();
            //var avaibleCourses = aviableCourse.Where(item => item.CourseStatus == true).ToList();
            filter.aviable = CourseRepository.ToCourseJsonDto(aviableCourse);

            return Json(filter, JsonRequestBehavior.AllowGet);
        }
        public void SendCompanyEmail(int companyId)
        {
            var comp = CompanyService.CompanyById(companyId);
            var profile = UserService.GetUserProfilesBy(comp.AdminId, FilterUserProfiles.ProfileId).FirstOrDefault();
            if (profile != null)
            {
                var param = new Dictionary<string, string>();
                param["USERNAME"] = profile.AspNetUsers.UserName;
                param["FNAME"] = profile.Firstname;
                param["KEY"] = profile.UserKey;
                param["URL"] = comp.Domain;
                EmailModel.inst.Send(comp.Email, "EyeBeep invitation", "userInvite", param, null);
            }
        }
        public ActionResult AddCourse(int masterCourseId, int companyId, int departmentId, int userId)
        {
            var course = CourseService.MasterCourse(masterCourseId);
            bool rez = false;
            if (userId != 0)
                rez = CourseService.BindCoursesTo(userId, FilterIdType.UserId, new List<MasterCourses> { course });
            else if (departmentId != 0)
                rez = CourseService.BindCoursesTo(departmentId, FilterIdType.DepartmentId, new List<MasterCourses> { course });
            else if (companyId != 0)
                rez = CourseService.BindCoursesTo(companyId, FilterIdType.CompanyId, new List<MasterCourses> { course });
            return Json( rez ? "Ok" : "Error", JsonRequestBehavior.AllowGet);
        }
        public ActionResult RemoveCourse(int masterCourseId, int companyId, int departmentId, int userId)
        {
            var course = CourseService.MasterCourse(masterCourseId);
            bool rez = false;
            if (userId != 0)
                rez = CourseService.UnbindCoursesFrom(userId, FilterIdType.UserId, course);
            else if (departmentId != 0)
                rez = CourseService.UnbindCoursesFrom(departmentId, FilterIdType.DepartmentId, course);
            else if (companyId != 0)
                rez = CourseService.UnbindCoursesFrom(companyId, FilterIdType.CompanyId, course);
            return Json(rez ? "Ok" : "Error", JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetUserTasks(int userId)
        {
            List<ReportObject> result = JobScheduler.inst.TasksByUserId(userId);
            return Json(result, JsonRequestBehavior.AllowGet);
        }
        public ActionResult RemoveTask(int id)
        {
            var sched = JobScheduler.inst.GetSchedulerByID(id);
            JobScheduler.inst.RemoveTask(sched);
            return Json("ok", JsonRequestBehavior.AllowGet);
        }
        public ActionResult Report(int course, int company, int department, int user, int reciverId, int interval, string startDate, string endDate)
        {
            string format = "dd.MM.yyyy";
            DateTime? start = null;
            DateTime? end = null;
            if (!startDate.Equals("")) start = DateTime.ParseExact(startDate, format, CultureInfo.InvariantCulture);
            if (!endDate.Equals("")) end = DateTime.ParseExact(endDate, format, CultureInfo.InvariantCulture);
            Portal.Web.Models.Scheduler schedul = new Portal.Web.Models.Scheduler()
            {
                Interval = interval,
                CompanyId = company,
                DepartmentId = department,
                UserId = user,
                ReciverId = reciverId,
                CourseId = course,
                StartDate = DateTime.Now.Date,
                FilterDateStart = start,
                FilterDateEnd = end
            };
            if (schedul.Interval != 3)
            {
                JobScheduler.inst.CreateTask(schedul);
            }
            else
            {
                JobScheduler.inst.Task(schedul);
            }
            return Json("task successfully added", JsonRequestBehavior.AllowGet);
        }
        public ActionResult ChangeDates(int course, int company, int department, int user, string start, string end)
        {
            bool rez = StatisticsRepository.ChangeDates(course, company, department, user, start, end);
            if (rez)
                return Json("Ok", JsonRequestBehavior.AllowGet);
            else
                return Json("Error", JsonRequestBehavior.AllowGet);
        }

        public ActionResult ValidateProfile(ProfileModel profileModel)
        {
            Dictionary<string, string> dic = new Dictionary<string, string>();
            string json = string.Empty;
            if (ModelState.IsValid == false)
            {
                var allKeys = Request.Form.AllKeys;
                for (int i = 0; i < allKeys.Count(); i++)
                {
                    ModelState error = (from val in ModelState.Values
                                        where val.Value == ModelState[allKeys[i]].Value
                                        select val).FirstOrDefault();
                    dic.Add(allKeys[i], string.Join("\n", error.Errors.Select(item => item.ErrorMessage)));
                }
                json = new JavaScriptSerializer().Serialize(dic);
            }
            else
            {
                if (profileModel.Photo != null) dic.Add("PhotoUpdate", "true");
                dic.Add("result", "submitted");
                new AdminController().Profile(profileModel);
                json = new JavaScriptSerializer().Serialize(dic);
            }

            return Json(json, JsonRequestBehavior.AllowGet);
        }
        public JsonResult ValidateUser(ProfileModel profileModel)
        {

            Dictionary<string, string> dic = new Dictionary<string, string>();
            string json = string.Empty;

            if (ModelState.IsValid == false)
            {
                var allKeys = ModelState.Keys.ToArray();
                for (int i = 0; i < allKeys.Count(); i++)
                {
                    ModelState error = (from val in ModelState.Values where val.Value == ModelState[allKeys[i]].Value select val).FirstOrDefault();
                    dic.Add(allKeys[i], string.Join("\n", error.Errors.Select(item => item.ErrorMessage)));
                }
                json = new JavaScriptSerializer().Serialize(dic);
            }
            else
            {
                dic.Add("result", "submitted");
                new AdminController().User(profileModel);
                json = new JavaScriptSerializer().Serialize(dic);
            }

            return Json(json, JsonRequestBehavior.AllowGet);
        }


    }
}
