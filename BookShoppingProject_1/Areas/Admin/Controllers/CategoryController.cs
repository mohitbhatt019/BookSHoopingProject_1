using BookShoppingProject_1.Data;
using BookShoppingProject_1.DataAccess.Repository.IRepository;
using BookShoppingProject_1.Models;
using BookShoppingProject_1.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BookShoppingProject_1.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles =SD.Role_Admin)]
    public class CategoryController : Controller
    {
        private readonly IUnitOfWork _UnitOfWork;
        public CategoryController(IUnitOfWork unitOfWork)
        {
            _UnitOfWork = unitOfWork;
        }
      //  [AllowAnonymous]
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Upsert(int? id)
        {
            Category category = new Category();
            if (id == null) return View(category);
            category = _UnitOfWork.Category.Get(id.GetValueOrDefault());
            if (category == null) return NotFound();
            return View(category);
        }
        [HttpPost]
        [AutoValidateAntiforgeryToken]
        public IActionResult Upsert(Category category)
        {
            if (category== null) return NotFound();
            if (!ModelState.IsValid) return View();
            if (category.ID == 0)
                _UnitOfWork.Category.Add(category);
            else
                _UnitOfWork.Category.Update(category);
            _UnitOfWork.Save();
            return RedirectToAction(nameof(Index));

        }
        #region APIs
        [HttpGet]
       // [AllowAnonymous]
        public IActionResult GetAll()
        {
            var categoryList = _UnitOfWork.Category.GetAll();
            return Json(new { data=categoryList});
        }
        [HttpDelete]
        public IActionResult Delete(int id)
        {
            var categoryInDb = _UnitOfWork.Category.Get(id);
            if (categoryInDb == null)
                return Json(new { success = false, message = "Something went wrong while deleteing data!!!" });
            _UnitOfWork.Category.Remove(categoryInDb);
            _UnitOfWork.Save();
            return Json(new { success = true, message = "Data Successfully Deleted!!!" });

        }
        #endregion
    }
}
