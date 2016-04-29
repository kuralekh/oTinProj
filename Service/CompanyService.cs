using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using NewDB;
using Newtonsoft.Json;
using Portal.Web.Helpers;
using Portal.Web.Models;
using Portal.Web.Models.DataObject;
using Portal.Web.NewRepository;
using Portal.Web.Services.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Portal.Web.Services
{
    public class CompanyService
    {
        private static readonly rCompanyRepository companyRepo = new rCompanyRepository();

        public static List<NewDB.Departments> GetDepartments(int companyId)
        {
            return companyRepo.GetDepartmentsByCompany(companyId);
        }
        public static List<NewDB.Departments> GetDepartments()
        {
            return companyRepo.GetAllDepartments();
        }
        public static NewDB.Departments GetDepartment(int depId)
        {
            return GetDepartments().Where(i => i.Id == depId).FirstOrDefault();
        }
        public static NewDB.Departments UpdateDepartment(int depId, string newName)
        {
            return companyRepo.UpdateDepartment(depId, newName);
        }
        public static void AddDepartment(NewDB.Departments department)
        {
            companyRepo.AddNewDepartment(department);
        }

        public static Companies CompanyById (int companyId)
        {
            return companyRepo.CompanyById(companyId);
        }
        public static Companies CompanyByDomain(string url)
        {
            return companyRepo.CompanyByDomain(url);
        }
        public static Companies CompanyByName(string name)
        {
            return companyRepo.CompanyByName(name);
        }
        public static List<Companies> GetAll()
        {
            return companyRepo.GetAll();
        }
        /// <summary>
        /// Get a list of Companies, which have at least one course
        /// </summary>
        /// <returns></returns>
        public static List<Companies> Authors()
        {
            return companyRepo.GetAll().Where (i => i.MasterCoursesAuthors.Any()).ToList();
        }
        public static Companies UpdateCompany(HttpServerUtilityBase server, HttpFileCollectionBase files, EditCompanyModel changes)
        {
            CustomizeCompany customize = new CustomizeCompany
            {
                StartVideo = changes.startVideo ?? "", 
                LogoUrl = changes.logoUrl ?? "", 
                LoginLogoUrl = changes.loginLogoUrl ?? "", 
                MenuActiveColor = changes.menuActiveColor ?? "",
                MenuPassiveColor = changes.menuPassiveColor ?? "",
                HeaderColor = changes.headerColor ?? "",
                ButtonsBackgroundColor = changes.buttonsBackgroundColor ?? "",
                ButtonsActiveColor = changes.buttonsActiveColor ?? ""
            };

            CompanyHelper.SaveFiles(server, files, changes.companyName, customize);

            string json = JsonConvert.SerializeObject(customize);
            
            return companyRepo.UpdateCompany(changes, json);
        }
        public static bool CheckNameAvailability(string companyName)
        {
            return companyRepo.CompanyByName(companyName) == null;
        }

        public static Companies AddCompany(HttpServerUtilityBase Server, HttpFileCollectionBase httpFileCollectionBase, EditCompanyModel model)
        {
            var UserManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(new ApplicationDbContext()));
            UserManager.UserValidator = new UserValidator<ApplicationUser>(UserManager) { AllowOnlyAlphanumericUserNames = false };

            Companies tmp = model.Companies;
            CustomizeCompany customize = new CustomizeCompany
            {
                StartVideo = model.startVideo ?? "", 
                LogoUrl = model.logoUrl ?? "", 
                LoginLogoUrl = model.loginLogoUrl ?? "", 
                MenuActiveColor = model.menuActiveColor ?? "",
                MenuPassiveColor = model.menuPassiveColor ?? "",
                HeaderColor = model.headerColor ?? "",
                ButtonsBackgroundColor = model.buttonsBackgroundColor ?? "",
                ButtonsActiveColor = model.buttonsActiveColor ?? ""
            };

            CompanyHelper.SaveFiles(Server, httpFileCollectionBase, model.companyName, customize);
            string pswd = CommonHelper.UserKey();

            tmp.CustomizeCompany = JsonConvert.SerializeObject(customize);
            tmp.TempPassword = pswd;
            tmp.Speech = model.speech;
            tmp.Lang = model.lang;
            tmp.CompanyStatus = (int)Status.NewlyCreated;
            tmp.Created = DateTime.Now;
            companyRepo.AddCompany(tmp);

            var department = new NewDB.Departments();
            department.CompanyId = tmp.Id;
            department.DepartmentName = CompanyHelper.InnitialDepartment;
            department = companyRepo.AddNewDepartment(department);
            //var userName = tmp.Name.Replace(" ", "_");
            
            var userName = tmp.ContactName + "_" + tmp.ContactSurname;
            var user = new ApplicationUser() { UserName = userName };
            var result = UserManager.Create(user, pswd);

            if (result.Succeeded)
            {
                UserManager.AddToRole(user.Id, "User");
                UserManager.AddToRole(user.Id, "Manager");
                UserManager.AddToRole(user.Id, "Administrator");
                var userProfile = new UserProfiles
                {
                    CompanyId = tmp.Id,
                    Email = tmp.Email,
                    Firstname = tmp.ContactName,
                    Lastname = tmp.ContactSurname,
                    Phone = tmp.ContactPhone,
                    AspNetUserId = user.Id,
                    DepartmentId = department.Id,
                    UserKey = CommonHelper.UserKey(),
                    Lang = 3,
                    Speech = 3
                };
                userProfile = UserService.AddUser(userProfile);
                //do not remove. It's magic spell, that makes entity work correctly
                userProfile.AspNetUsers = new AspNetUsers { UserName = user.UserName, Id = user.Id };
                tmp.AdminId = userProfile.Id;
            }
            return tmp;
        }
        public static void DeleteDepartment(int depId)
        {
            companyRepo.DeleteDepartment(depId);
        }
        public static List<string> DeleteCompany(int id)
        {
            return companyRepo.DeleteCompany(id);
        }
        public static void ChangeCompanyStatus(int id, Status status)
        {
            companyRepo.ChangeCompanyStatus(id, status);
        }


    }
}