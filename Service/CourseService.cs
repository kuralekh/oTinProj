using NewDB;
using Portal.Web.Models.DataObject;
using Portal.Web.NewRepository;
using Portal.Web.Services.Filters;
using Portal.Web.Services.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Portal.Web.Services
{
    public class CourseService
    {
        private static readonly rCourseRepository courseRepo = new rCourseRepository();
#region Master courses
        public static List<MasterCourses> GetAllMasterCourses()
        {
            return courseRepo.MasterCourses();
        }
        public static MasterCourses MasterCourse(int courseId)
        {
            return courseRepo.MasterCourses(new FilterCourses { CourseID = courseId }).FirstOrDefault();
        }
        public static List<MasterCourses> MasterCoursesAssignedTo(int Id, FilterIdType type, bool showDeactivated = true)
        {
            switch (type)
            {
                case FilterIdType.CompanyId:
                    return courseRepo.MasterCourses(new FilterCourses { AssignedToCompanyIds = new List<int> { Id } }, showDeactivated);
                case FilterIdType.DepartmentId:
                    return courseRepo.MasterCourses(new FilterCourses { AssignedToDepartmentIds = new List<int> { Id } }, showDeactivated);
                case FilterIdType.UserId:
                    return courseRepo.MasterCourses(new FilterCourses { AssignedToUserIds = new List<int> { Id } }, showDeactivated);
                default:
                    return new List<MasterCourses>();
            }
        }
        /// <summary>
        /// Get avaliable courses in shop for company, department, or user.
        /// </summary>
        /// <param name="companyId"></param>
        /// <param name="departmentId"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public static List<MasterCourses> MasterCoursesAvaliableTo (int Id, FilterIdType type)
        {
            var assigned = new List<MasterCourses>();
            switch (type)
            {
                case FilterIdType.CompanyId:
                    assigned = courseRepo.MasterCourses(new FilterCourses { AssignedToCompanyIds = new List<int> { Id } });
                    break;
                case FilterIdType.DepartmentId:
                    assigned = courseRepo.MasterCourses(new FilterCourses { AssignedToDepartmentIds = new List<int> { Id } });
                    break; 
                case FilterIdType.UserId:
                    assigned = courseRepo.MasterCourses(new FilterCourses { AssignedToUserIds = new List<int> { Id } });
                    break;
                default:
                    return new List<MasterCourses>();
            }
            return courseRepo.MasterCourses().Except(assigned).ToList();
        }
        public static bool BindCoursesTo(int id, FilterIdType type, List<MasterCourses> courses)
        {
            switch (type)
            {
                case FilterIdType.CompanyId:
                    return courseRepo.BindCoursesToCompany(id, courses);
                case FilterIdType.DepartmentId:
                    return courseRepo.BindCoursesToDepartment(id, courses);
                case FilterIdType.UserId:
                    return courseRepo.BindCoursesToUser(id, courses);
                default:
                    return false;
            }
        }
        public static bool UnbindCoursesFrom(int id, FilterIdType type, MasterCourses course)
        {
            switch (type)
            {
                case FilterIdType.CompanyId:
                    return courseRepo.UnbindCoursesFromCompany(id, course);
                case FilterIdType.DepartmentId:
                    return courseRepo.UnbindCoursesFromDepartment(id, course);
                case FilterIdType.UserId:
                    return courseRepo.UnbindCoursesFromUser(id, course);
                default:
                    return false;
            }
        }
        public static bool ChangeCourseStatusFor(int id, FilterIdType type, int courseId, bool status)
        {
            switch (type)
            {
                case FilterIdType.CompanyId:
                    return courseRepo.ChangeCourseStatusCompany(id, courseId, status);
                case FilterIdType.DepartmentId:
                    return courseRepo.ChangeCourseStatusDepartment(id, courseId, status);
                case FilterIdType.UserId:
                    return courseRepo.ChangeCourseStatusUser(id, courseId, status);
                default:
                    return false;
            }   
        }
        /// <summary>
        /// Get a Company, Department, or UserCourse with all needed info
        /// </summary>
        /// <param name="id">id of Company, Department or User who has assigned course</param>
        /// <param name="type">type of Id</param>
        /// <param name="courseId">id of MasterCourse </param>
        /// <returns>Cast Object to a needed type of Course</returns>
        public static object GetCourse(int id, FilterIdType type, int courseId)
        {
            switch (type)
            {   
                case FilterIdType.CompanyId:
                    return courseRepo.CompanyCourse(courseId, id);
                case FilterIdType.DepartmentId:
                    return courseRepo.DepartmentCourse(courseId, id);
                case FilterIdType.UserId:
                    return courseRepo.UserCourse(courseId, id);
                default:
                    return null;
            }
        }
        public static bool GetCourseStatus(int id, FilterIdType type, int courseId)
        {
            switch (type)
            {
                case FilterIdType.CompanyId:
                    return (GetCourse(id, type, courseId) as CompanyCourses).CourseStatus;
                case FilterIdType.DepartmentId:
                    return (GetCourse(id, type, courseId) as DepartmentCourses).CourseStatus;
                case FilterIdType.UserId:
                    return (GetCourse(id, type, courseId) as UserCourses).CourseStatus;
                default:
                    return false;
            }
        }
        public static void ChangeMasterCourseOrder(List<CoursesOrder> orderlist)
        {
            courseRepo.ChangeMasterCourseOrder(orderlist);
        }
#endregion
        #region Statistic
        public static List<CoursesStatDto> GetCoursesForTableStat(int Id, FilterIdType type)
        {
            var list = new List<CoursesStatDto>();
            FilterCourses filter = null;
            switch (type)
            {
                case FilterIdType.CompanyId:
                    filter = new FilterCourses { AssignedToCompanyIds = new List<int> { Id } };
                    break;
                case FilterIdType.DepartmentId:
                    filter = new FilterCourses { AssignedToDepartmentIds = new List<int> { Id } };
                    break;
                case FilterIdType.UserId:
                    filter = new FilterCourses { AssignedToUserIds = new List<int> { Id } };
                    break;
                case FilterIdType.All:
                    filter = null;
                    break;
            }
            list = courseRepo.GetAssignedCoursesForStatistic(filter);

            var userCourses = courseRepo.UserCourses(filter);
            var userIds = userCourses.Select(u => u.UserId.Value).ToList();
            foreach (var entry in list)
            {
                //users
                entry.Users = userCourses.Where(i => i.MasterCourseId == entry.MasterCourseId).Count();
                //avg score
                var maxScores = userCourses.Where(i => i.MasterCourseId == entry.MasterCourseId && i.CompletedDate.HasValue).Select(c => c.MaxUserScore);
                entry.Avg = maxScores.Any() ? maxScores.Average() : 0;
                //status of completion
                var watchedcourse = userCourses.Where(i => i.MasterCourseId == entry.MasterCourseId).FirstOrDefault();
                var watched = watchedcourse != null ? watchedcourse.MasterCourses.UserModulesWatched.Where (i => userIds.Contains(i.UserProfiles.Id)).Count() : 0;
                entry.Status = entry.Users == 0 || entry.Modules == 0 ? 0 : watched * 100 / (entry.Users * entry.Modules);
            }
            return list;
        }
        public static List<UserCourses> GetPassedExams (FilterCourses filter)
        {
            return courseRepo.GetPassedCourses(filter);
        }
        #endregion
        #region Modules
        public static List<Modules> ModulesAvaliableByCourseId(int courseId)
        {
            return courseRepo.ModulesByCourseId(courseId).OrderBy(i => i.OrderNum).ToList();
        }
        public static bool SetModuleSeen(int userId, int courseId, int moduleId)
        {
            try
            {
                courseRepo.SetLastSeenModule(userId, courseId, moduleId);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        #endregion
        #region Company, Department, User Courses
        public static List<UserCourses> UserCourses(int profileId)
        {
            return courseRepo.UserCourses(new FilterCourses { AssignedToUserIds = new List<int> { profileId } });
        }
        public static void AddTryToUserCourse(int usercourseId)
        {
            courseRepo.AddTry(usercourseId);
        }
        #endregion

        public static List<CourseCategorys> Categories()
        {
            return courseRepo.Categories().ToList();
        }
    }
}