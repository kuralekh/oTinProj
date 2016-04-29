using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using NewDB;
using Newtonsoft.Json;
using Portal.Web.Helpers;
using Portal.Web.Models;
using Portal.Web.Models.DataObject;
using Portal.Web.Services.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace Portal.Web.NewRepository
{
    public class rCompanyRepository
    {
        private NewEyeBeepEntities db = DBContextWrapper.DB;
        private rUserRepository userRepo = new rUserRepository();
        public List<Companies> GetAll()
        {
            return db.Companies.ToList();
        }
        public Companies CompanyById(int id)
        {
            return db.Companies.FirstOrDefault(x => x.Id == id);
        }

        public Companies CompanyByName(string NameCompany)
        {
            return db.Companies.Where(i => i.Name == NameCompany).FirstOrDefault();
        }

        public Companies CompanyByUserName(string username)
        {

            var user = db.AspNetUsers.FirstOrDefault(u => u.UserName == username);

            if (user == null)
                return null;

            var profile = user.UserProfiles.FirstOrDefault();

            if (profile == null)
                return null;

            return profile.Companies;
        }

        public Companies CompanyByDomain(string domian)
        {
           return db.Companies.FirstOrDefault(a => a.Domain == domian) ??
                       db.Companies.FirstOrDefault(a => a.Name == CompanyHelper.DefaultCompany);
        }

        public List<NewDB.Departments> GetDepartmentsByCompany(int companyId)
        {
            return db.Departments.Where(i => i.CompanyId == companyId).ToList();
        }
        public List<NewDB.Departments> GetAllDepartments()
        {
            return db.Departments.ToList();
        }
        public NewDB.Departments UpdateDepartment(int depId, string newName)
        {
            var dep = db.Departments.Where(i => i.Id == depId).FirstOrDefault();
            if (dep != null)
            {
                dep.DepartmentName = newName;
                db.SaveChanges();
            }
            return dep;
        }
        public void SetSoundStatusToUsers(Companies comp)
        {
            var userList = comp.UserProfiles.ToList();
            userList.ForEach(u => u.SoundStatus = comp.SoundStatus);
            try
            {
                db.SaveChanges();
            }
            catch (Exception) 
            {
                throw new Exception("While saving sound status for users, some mistake occured") 
                { 
                    Source = "SetSoundStatusToUsers" 
                };
            }
        }
        public void ChangeCompanyStatus(int id, Status status)
        {
            var company = CompanyById(id);
            if (company == null) return;
            var users = company.UserProfiles;
            foreach (var user in users)
            {
                if (!userRepo.IsUserInRole(user.AspNetUsers.UserName, RolesNames.superadmin))
                    user.UserStatus = (int) status;
            }
            company.CompanyStatus = (int) status;
            try { db.SaveChanges(); }
            catch (Exception) 
            {
                throw new Exception("While saving status of company, some mistake occured")
                {
                    Source = "ChangeCompanyStatus"
                };
            }
            
        }
        public NewDB.Departments AddNewDepartment(NewDB.Departments department)
        {
            if (department.CompanyId == 0) return null;
            db.Departments.Add(department);

            //binding company courses to the dep
            var company = db.Companies.Where(c => c.Id == department.CompanyId).FirstOrDefault();
            if (company != null)
            {
                int order = 1;
                foreach (var course in company.CompanyCourses)
                    db.DepartmentCourses.Add(new DepartmentCourses()
                    {
                        CourseId = course.CourseId,
                        CourseStatus = course.CourseStatus,
                        DepartmentId = department.Id,
                        EndDate = course.EndDate,
                        StartDate = course.StartDate,
                        OrderNum = order++
                    });
            }
            db.SaveChanges();
            db.Entry(department).Reload();
            return department;
        }
        public void DeleteDepartment(int id)
        {
            var department = db.Departments.FirstOrDefault(d => d.Id == id);

            if (department != null)
            {
                var users = db.UserProfiles.Where(u => u.CompanyId == department.CompanyId && u.DepartmentId == department.Id).ToList();
                userRepo.DeleteUsers(users.Select(i => i.Id).ToList());

                var depCourses = db.DepartmentCourses.Where(d => d.DepartmentId == department.Id).ToList();
                if (depCourses.Any())
                    db.DepartmentCourses.RemoveRange(depCourses);
                department = db.Departments.FirstOrDefault(d => d.Id == id);
                
                db.Departments.Remove(department);
                
                try { db.SaveChanges(); }
                catch (Exception) { throw new Exception("While deleting a department an error occured"); }
            }
        }
        public void AddCompany(Companies newCompany)
        {
            db.Companies.Add(newCompany);
            db.SaveChanges();
        }

        public List<string> DeleteCompany(int id)
        {

            var company = CompanyById(id);
            var departments = company.Departments.ToList();
            var compCourses = company.CompanyCourses.ToList();
            var repo = new rUserRepository();
            var usersId = db.UserProfiles.Where(k => k.CompanyId == id).Select(i => i.Id).ToList();

            repo.DeleteUsers(usersId);
            //string pathToProfile = CommonHelper.PathRoot + PathToSave + userprofile.AspNetUsers.UserName;
            //if (Directory.Exists(pathToProfile))
            //    Directory.Delete(pathToProfile, true);

            //foreach (var userprofile in usersId)
            //{
            //    var aspNetUser = userprofile.AspNetUsers;
            //    var usercourses = userprofile.UserCourses.ToList();
            //    var exams = userprofile.Exams.ToList();
            //    var watchedmodules = userprofile.UserModulesWatched.ToList();
            //    var answers = userprofile.UserAnswers.ToList();

            //    if (answers.Any())
            //        db.UserAnswers.RemoveRange(answers);
            //    if (watchedmodules.Any())
            //        db.UserModulesWatched.RemoveRange(watchedmodules);
            //    if (usercourses.Any())
            //        db.UserCourses.RemoveRange(usercourses);
            //    if (exams.Any())
            //        db.Exams.RemoveRange(exams);
            //    db.UserProfiles.Remove(userprofile);
            //    db.AspNetUsers.Remove(aspNetUser);
            //}
            
            //db.UserProfiles.RemoveRange(usersId);
            //db.SaveChanges();
            db.Departments.RemoveRange(departments);
            //foreach (var department in departments)
            //{
            //    DeleteDepartment(department.Id);

            //}
            if (compCourses.Any())
                db.CompanyCourses.RemoveRange(compCourses);
            try
            {
                db.Entry(company).Reload();
                db.Companies.Remove(company);
                db.SaveChanges();
            }
            catch (Exception)
            {
                List<string> message = new List<string>();
                message.Add("Can't delete the company " + company.Name + ". ");
                var allmastercourses = db.MasterCourses;
                var courses = allmastercourses.Where(i => i.AuthorId == id).ToList();
                if (courses.Any())
                {
                    message.Add("Referenced MasterCourses: ");
                    message.Add(String.Join(", ", courses.Select(i => i.Name)));
                }
                var modules = db.Modules.Where(i => i.AuthorId == id)
                                                .Select(w => w.Title).ToList();
                if (modules.Any())
                {
                    message.Add("Referenced Modules: ");
                    message.Add(String.Join(", ", modules));
                }
                return message;
            }
            return new List<string> { "ok" };
        }

        public Companies UpdateCompany(EditCompanyModel changes, string jsonCustomize)
        {
            var company = db.Companies.Where(i => i.Id == changes.CompanyId).FirstOrDefault();
            if (company == null)
                return null;

            company.Domain = changes.domain.Replace("http://", "");
            company.CompanyPhone = changes.phone;
            company.Description = changes.description;
            company.ContactName = changes.contactName;
            company.ContactSurname = changes.contactSurname;
            company.ContactPhone = changes.contactPhone;
            company.BillinAddress = changes.billinAddress;
            company.City = changes.city;
            company.CompanySite = changes.companySite;
            company.Name = changes.companyName;
            company.OrgNummer = changes.orgNummer.ToString();
            company.ZipCode = changes.zipCode;
            company.Email = changes.email;
            company.Country = changes.country;
            company.Speech = changes.speech == 0 ? 1 : changes.speech;
            company.Lang = changes.lang == 0 ? 1 : changes.lang;
            company.SoundStatus = changes.SoundStatus;
            company.CustomizeCompany = jsonCustomize;
            db.SaveChanges();
            SetSoundStatusToUsers(company);
            return company;
        }


    }
}