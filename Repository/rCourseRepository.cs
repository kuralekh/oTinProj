using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using NewDB;
using Portal.Web.Models.DataObject;
using Portal.Web.Services.Filters;
namespace Portal.Web.NewRepository
{
    public class rCourseRepository
    {
        private NewEyeBeepEntities db = DBContextWrapper.DB;
#region MasterCourses
        /// <summary>
        /// Get the list of mastercourses, assigned to company, department or user. If filter is null - returns all entries
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        public List<MasterCourses> MasterCourses(FilterCourses filter = null, bool showDeactivated = true)
        {
            var list = db.MasterCourses.ToList();
            if (filter == null)
                return list;
            //filter rezulting list step by step. 
            if (filter.AssignedToCompanyIds.Count > 0)
                list = list.Where(i => i.CompanyCourses.Where(c => filter.AssignedToCompanyIds.Contains(c.CompanyId) && (showDeactivated == true || c.CourseStatus == true)).Any()).ToList();
            if (filter.AssignedToDepartmentIds.Count > 0)
                list = list.Where(i => i.DepartmentCourses.Where(c => filter.AssignedToDepartmentIds.Contains(c.DepartmentId) && (showDeactivated == true || c.CourseStatus == true)).Any()).ToList();
            if (filter.AssignedToUserIds.Count > 0)
                list = list.Where(i => i.UserCourses.Where(c => c.UserId.HasValue == true && filter.AssignedToUserIds.Contains(c.UserId.Value) && (showDeactivated == true || c.CourseStatus == true)).Any()).ToList();
            if (filter.CourseID != null)
                list = list.Where(i => i.Id == filter.CourseID).ToList();
            return list;
        }
        public List<CoursesStatDto> GetAssignedCoursesForStatistic(FilterCourses filter)
        {
            if (filter == null)
            {
                var courses = db.MasterCourses.ToList();
                return courses.Select(i => new CoursesStatDto
                    {
                        CourseName = i.Name,
                        Cover = i.CoverUrl,
                        Deadline = !i.EndTime.HasValue ? "" : i.EndTime.Value.ToString(),
                        MasterCourseId = i.Id,
                        Modules = i.MasterCourseModules.Where (c=> c.MasterCourseId == i.Id).Count(),
                        Start = !i.StartTime.HasValue ? "" : i.StartTime.Value.ToString()
                    }).ToList();
            }
            else if (filter.AssignedToCompanyIds.Count > 0)
            {
                var courses = db.CompanyCourses.Where(c => filter.AssignedToCompanyIds.Contains(c.CompanyId)).ToList();
                return courses.Select(i => new CoursesStatDto
                    {
                        CourseName = i.MasterCourses.Name,
                        Cover = i.MasterCourses.CoverUrl,
                        Deadline = !i.EndDate.HasValue ? "" : i.EndDate.Value.ToString(),
                        MasterCourseId = i.CourseId,
                        Modules = i.MasterCourses.MasterCourseModules.Where(c => c.MasterCourseId == i.Id).Count(),
                        Start = !i.StartDate.HasValue ? "" : i.StartDate.Value.ToString()
                    })
                    .ToList();
            }
            else if (filter.AssignedToDepartmentIds.Count > 0)
            {
                var courses = db.DepartmentCourses.Where(c => filter.AssignedToDepartmentIds.Contains(c.DepartmentId)).ToList(); 
                return courses.Select(i => new CoursesStatDto
                    {
                        CourseName = i.MasterCourses.Name,
                        Cover = i.MasterCourses.CoverUrl,
                        Deadline = !i.EndDate.HasValue ? "" : i.EndDate.Value.ToString(),
                        MasterCourseId = i.CourseId,
                        Modules = i.MasterCourses.MasterCourseModules.Where(c => c.MasterCourseId == i.Id).Count(),
                        Start = !i.StartDate.HasValue ? "" : i.StartDate.Value.ToString()
                    })
                    .ToList();
            }
            else if (filter.AssignedToUserIds.Count > 0)
            {
                var courses = db.UserCourses.Where(c => c.UserId.HasValue && filter.AssignedToUserIds.Contains(c.UserId.Value)).ToList();
                return courses.Select(i => new CoursesStatDto
                    {
                        CourseName = i.MasterCourses.Name,
                        Cover = i.MasterCourses.CoverUrl,
                        Deadline = !i.EndDate.HasValue ? "" : i.EndDate.Value.ToString(),
                        MasterCourseId = i.MasterCourseId,
                        Modules = i.MasterCourses.MasterCourseModules.Where(c => c.MasterCourseId == i.Id).Count(),
                        Start = !i.StartDate.HasValue ? "" : i.StartDate.Value.ToString()
                    })
                    .ToList();
            }
            else return new List<CoursesStatDto>();
            
        }
        public List<MasterCourses> GetAvaliableCourses(FilterCourses filter)
        {
            var rez = db.MasterCourses.ToList();
            if (filter == null)
                return rez;
            else if (filter.AssignedToCompanyIds.Count > 0)
            {
                var assigned = db.CompanyCourses.Where (i =>filter.AssignedToCompanyIds.Contains(i.CompanyId)).Select(s => s.MasterCourses);
                return rez.Except(assigned).ToList();
            }
            else if (filter.AssignedToDepartmentIds.Count > 0)
            {
                var assigned = db.DepartmentCourses.Where(i => filter.AssignedToDepartmentIds.Contains(i.DepartmentId)).Select(s => s.MasterCourses);
                return rez.Except(assigned).ToList();
            }
            else if (filter.AssignedToUserIds.Count > 0)
            {
                var assigned = db.UserCourses.Where(i => filter.AssignedToUserIds.Contains(i.UserId.Value)).Select(s => s.MasterCourses);
                return rez.Except(assigned).ToList();
            }
            return rez;
        }
        public void ChangeMasterCourseOrder(List<CoursesOrder> order)
        {
            var courses = db.MasterCourses;
            foreach (var c in order)
            {
                courses.FirstOrDefault(i => i.Id == c.id).OrderNum = c.orderNum;
            }
            db.SaveChanges();
        }
#endregion
#region Company, Department, User Courses
        public CompanyCourses CompanyCourse(int courseId, int compId)
        {
            return db.CompanyCourses.FirstOrDefault(i => i.CourseId == courseId && i.CompanyId == compId);
        }
        public DepartmentCourses DepartmentCourse(int courseId, int depId)
        {
            return db.DepartmentCourses.FirstOrDefault(i => i.CourseId == courseId && i.DepartmentId == depId);
        }
        public UserCourses UserCourse(int courseId, int userId)
        {
            return db.UserCourses.FirstOrDefault(i => i.MasterCourseId == courseId && i.UserId == userId);
        }
        public List<UserCourses> UserCourses (FilterCourses filter)
        {
            //if we don't need to filter, filter is null, so return all entries
            if (filter == null)
                return db.UserCourses.Where(i => i.UserId.HasValue).ToList();
            else if (filter.AssignedToCompanyIds.Count > 0)
                return db.UserCourses.Where(i => filter.AssignedToCompanyIds.Contains(i.UserProfiles.CompanyId)).ToList();
            else if (filter.AssignedToDepartmentIds.Count > 0)
                return db.UserCourses.Where(i => filter.AssignedToDepartmentIds.Contains(i.UserProfiles.DepartmentId)).ToList();
            else if (filter.AssignedToUserIds.Count > 0)
                return db.UserCourses.Where(i => filter.AssignedToUserIds.Contains(i.UserId.Value)).ToList();
            else
                throw new KeyNotFoundException("No such filter configuration found :(");

        }
        public void AddTry(int userCourseId)
        {
            var course = db.UserCourses.Where(i => i.Id == userCourseId).First();
            if (course.NumTries == null)
                course.NumTries = 1;
            else
                course.NumTries++;
            try
            {
                db.SaveChanges();
            }
            catch (Exception)
            {
                throw new Exception("Cant save changes, while adding a try to user course");
            }
        }
        public List<UserCourses> GetPassedCourses(FilterCourses filter)
        {
            var courses = db.UserCourses.Where(i => i.HasPassed.HasValue && i.HasPassed == true 
                && i.CompletedDate.HasValue
                && i.CompletedDate>= filter.StartDate && i.CompletedDate <= filter.EndDate).ToList();
            if (filter.CourseID != 0)
                courses = courses.Where(i => i.MasterCourseId == filter.CourseID).ToList();
            if (filter.AssignedToCompanyIds.Count > 0)
                courses = courses.Where(i => filter.AssignedToCompanyIds.Contains(i.UserProfiles.CompanyId)).ToList();
            if (filter.AssignedToDepartmentIds.Count > 0)
                courses = courses.Where(i => filter.AssignedToDepartmentIds.Contains(i.UserProfiles.DepartmentId)).ToList();
            if (filter.AssignedToUserIds.Count > 0)
                courses = courses.Where(i => filter.AssignedToUserIds.Contains(i.UserId.Value)).ToList();
           
            return courses;
        }
#endregion
#region binding/unbinding courses
        /// <summary>
        /// Bind courses to a company, to all departments and users in it
        /// </summary>
        /// <param name="depId"></param>
        /// <param name="courses"></param>
        /// <returns></returns>
        public bool BindCoursesToCompany(int compId, List<MasterCourses> courses)
        {
            var company = db.Companies.FirstOrDefault(i => i.Id == compId);
            if (company == null) return false;

            var compCourses = company.CompanyCourses;
            var departments = company.Departments;

            var userProfiles = company.UserProfiles;

            foreach (var c in courses)
            {
                //binding company courses
                compCourses.Add(new CompanyCourses 
                {
                    CompanyId = compId,
                    CourseId = c.Id,
                    CourseStatus = true,
                    StartDate = c.StartTime,
                    EndDate = c.EndTime,
                    OrderNum = 0
                });

                //binding department courses
                foreach (var d in departments)
                {
                    d.DepartmentCourses.Add(new DepartmentCourses
                    {
                        CourseId = c.Id,
                        OrderNum = 0,
                        CourseStatus = true,
                        StartDate = c.StartTime,
                        EndDate = c.EndTime,
                        DepartmentId = d.Id
                    });
                }
                //bind usercourses
                foreach (var user in userProfiles)
                {
                    user.UserCourses.Add(new UserCourses
                    {
                        MasterCourseId = c.Id,
                        MaxUserScore = 0,
                        NumTries = 0,
                        OrderNum = 0,
                        UserId = user.Id,
                        CourseStatus = true,
                        StartDate = c.StartTime,
                        EndDate = c.EndTime
                    });
                }
            }
            try
            { db.SaveChanges(); }
            catch (Exception) { return false; }
            return true;
        }
        /// <summary>
        /// Bind courses to department and all users in it
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="courses"></param>
        /// <returns></returns>
        public bool BindCoursesToDepartment(int depId, List<MasterCourses> courses)
        {
            var department = db.Departments.Where(i => i.Id == depId).FirstOrDefault();
            if (department == null) return false;
            var depCourses = department.DepartmentCourses;

            var userProfiles = department.UserProfiles;

            foreach (var c in courses)
            {
                //binding department courses
                depCourses.Add(new DepartmentCourses
                    {
                        CourseId = c.Id,
                        OrderNum = 0,
                        CourseStatus = true,
                        StartDate = c.StartTime,
                        EndDate = c.EndTime,
                        DepartmentId = depId
                    });
                //bind usercourses
                foreach (var user in userProfiles)
                {
                    user.UserCourses.Add(new UserCourses
                    {
                        MasterCourseId = c.Id,
                        MaxUserScore = 0,
                        NumTries = 0,
                        OrderNum = 0,
                        UserId = user.Id,
                        CourseStatus = true,
                        StartDate = c.StartTime,
                        EndDate = c.EndTime
                    });
                }
            }
            try
            { db.SaveChanges(); }
            catch (Exception) { return false; }
            return true;
        }
        public bool BindCoursesToUser(int userId, List <MasterCourses> courses)
        {
            var userProfile = db.UserProfiles.Where(i => i.Id == userId).FirstOrDefault();
            if (userProfile == null) return false;

            var userCourses = userProfile.UserCourses;

            foreach (var c in courses)
            {
                userCourses.Add(new UserCourses 
                {
                    MasterCourseId = c.Id,
                    MaxUserScore = 0,
                    NumTries = 0,
                    OrderNum = 0,
                    UserId = userId,
                    CourseStatus = true,
                    StartDate = c.StartTime,
                    EndDate = c.EndTime
                });
            }
            try
            { db.SaveChanges(); }
            catch (Exception) { return false; }
            return true;
        }
        public bool UnbindCoursesFromCompany(int compId, MasterCourses course)
        {
            var company = db.Companies.FirstOrDefault(i => i.Id == compId);
            if (company == null) return false;

            var compCourses = company.CompanyCourses.Where(i => i.CourseId == course.Id);
            if (compCourses.Any())
                db.CompanyCourses.RemoveRange(compCourses);

            var depCourses = company.Departments.SelectMany(i => i.DepartmentCourses).Where(w => w.CourseId == course.Id);
            if (depCourses.Any())
                db.DepartmentCourses.RemoveRange(depCourses);

            var userProfiles = company.UserProfiles;
            foreach (var user in userProfiles)
            {
                var watched = user.UserModulesWatched.Where(i => i.CourseID == course.Id);
                if (watched.Any())
                    db.UserModulesWatched.RemoveRange(watched);

                var exams = user.Exams.Where(i => i.CourseId == course.Id);
                if (exams.Any())
                {
                    var answers = exams.SelectMany(i => i.UserAnswers);
                    if (answers.Any())
                        db.UserAnswers.RemoveRange(answers);
                    db.Exams.RemoveRange(exams);
                }
                var userCourses = user.UserCourses.Where(i => i.MasterCourseId == course.Id);
                if (userCourses.Any())
                    db.UserCourses.RemoveRange(userCourses);
            }
            try{db.SaveChanges();}
            catch(Exception){return false;}
            return true;
        }
        public bool UnbindCoursesFromDepartment(int depId, MasterCourses course)
        {
            var department = db.Departments.FirstOrDefault(i => i.Id == depId);
            if (department == null)
                return false;
            var depCourses = department.DepartmentCourses.Where(w => w.CourseId == course.Id);
            if (depCourses.Any())
                db.DepartmentCourses.RemoveRange(depCourses);

            var userProfiles = department.UserProfiles;
            foreach (var user in userProfiles)
            {
                var watched = user.UserModulesWatched.Where(i => i.CourseID == course.Id);
                if (watched.Any())
                    db.UserModulesWatched.RemoveRange(watched);

                var exams = user.Exams.Where(i => i.CourseId == course.Id);
                if (exams.Any())
                {
                    var answers = exams.SelectMany(i => i.UserAnswers);
                    if (answers.Any())
                        db.UserAnswers.RemoveRange(answers);
                    db.Exams.RemoveRange(exams);
                }
                var userCourses = user.UserCourses.Where(i => i.MasterCourseId == course.Id);
                if (userCourses.Any())
                    db.UserCourses.RemoveRange(userCourses);
            }
            try { db.SaveChanges(); }
            catch (Exception) { return false; }
            return true;
        }
        public bool UnbindCoursesFromUser(int userId, MasterCourses course)
        {
            var user = db.UserProfiles.FirstOrDefault(i => i.Id == userId);
            if (user == null)
                return false;
            
            var watched = user.UserModulesWatched.Where(i => i.CourseID == course.Id);
            if (watched.Any())
                db.UserModulesWatched.RemoveRange(watched);

            var exams = user.Exams.Where(i => i.CourseId == course.Id);
            if (exams.Any())
            {
                var answers = exams.SelectMany(i => i.UserAnswers);
                if (answers.Any())
                    db.UserAnswers.RemoveRange(answers);
                db.Exams.RemoveRange(exams);
            }
            var userCourses = user.UserCourses.Where(i => i.MasterCourseId == course.Id);
            if (userCourses.Any())
                db.UserCourses.RemoveRange(userCourses);
            
            try { db.SaveChanges(); }
            catch (Exception) { return false; }
            return true;
        }
#endregion
        public List<Modules> ModulesByCourseId(int courseId)
        {
            return db.MasterCourseModules.Where(i => i.MasterCourseId == courseId).Select(s => s.Modules).ToList();
        }
        public void SetLastSeenModule(int userId, int courseId, int moduleId)
        {
            var user = db.UserProfiles.FirstOrDefault(u => u.Id == userId);

            var moduleWatched = user.UserModulesWatched.FirstOrDefault(
                m => m.ModuleId == moduleId &&
                m.UserProfileId == user.Id &&
                m.CourseID == courseId);

            if (moduleWatched == null)
            {
                moduleWatched = new UserModulesWatched
                {
                    CourseID = courseId,
                    ModuleId = moduleId,
                    UserProfileId = user.Id,
                    startDate = DateTime.Now
                };
                db.UserModulesWatched.Add(moduleWatched);
                db.SaveChanges();
            }
        }
        public List<CourseCategorys> Categories()
        {
            return db.CourseCategorys.ToList();
        }
        public bool ChangeCourseStatusCompany(int compId, int courseId, bool status)
        {
            var company = db.Companies.FirstOrDefault(i => i.Id == compId);
            if (company == null) return false;

            var compCourse = company.CompanyCourses.FirstOrDefault(i => i.CourseId == courseId);
            if (compCourse == null) return false;

            compCourse.CourseStatus = status;
            //changing status of department courses to deactivated
            var depCourses = company.Departments.SelectMany(i => i.DepartmentCourses).Where(s => s.CourseId == courseId).ToList();
            if (depCourses.Any())
                depCourses.ForEach(i => i.CourseStatus = status);

            //changing status of user courses to deactivated
            var userCourses = company.UserProfiles.SelectMany(i => i.UserCourses).Where(s => s.MasterCourseId == courseId).ToList();
            if (userCourses.Any())
                userCourses.ForEach(i => i.CourseStatus = status);

            try { db.SaveChanges(); }
            catch (Exception)
            { return false; }

            return true;
        }
        public bool ChangeCourseStatusDepartment(int depId, int courseId, bool status)
        {
            var department = db.Departments.FirstOrDefault(i => i.Id == depId);
            if (department == null) return false;

           
            //changing status of department courses to deactivated
            var depCourses = department.DepartmentCourses.Where(s => s.CourseId == courseId).ToList();
            if (depCourses.Any())
                depCourses.ForEach(i => i.CourseStatus = status);

            //changing status of user courses to deactivated
            var userCourses = department.UserProfiles.SelectMany(i => i.UserCourses).Where(s => s.MasterCourseId == courseId).ToList();
            if (userCourses.Any())
                userCourses.ForEach(i => i.CourseStatus = status);

            try { db.SaveChanges(); }
            catch (Exception)
            { return false; }

            return true;
        }
        public bool ChangeCourseStatusUser(int userId, int courseId, bool status)
        {
            var user = db.UserProfiles.FirstOrDefault(i => i.Id == userId);
            if (user == null) return false;

            //changing status of user courses to deactivated
            var userCourses = user.UserCourses.Where(s => s.MasterCourseId == courseId).ToList();
            if (userCourses.Any())
                userCourses.ForEach(i => i.CourseStatus = status);

            try { db.SaveChanges(); }
            catch (Exception)
            { return false; }

            return true;
        }
    }
}