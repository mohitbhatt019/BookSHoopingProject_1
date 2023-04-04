using BookShoppingProject_1.DataAccess.Repository.IRepository;
using BookShoppingProject_1.Models;
using BookShoppingProject_1.Models.ViewModels;
using BookShoppingProject_1.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace BookShoppingProject_1.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles =SD.Role_Admin)]
    public class ProductController : Controller
    {
        private readonly IUnitOfWork _UnitOfWork;
        private readonly IWebHostEnvironment _webHostEnvironment; //Provides information about the web hosting environment (www path)
        public ProductController(IUnitOfWork unitOfWork,IWebHostEnvironment webHostEnvironment   )

        {
            _UnitOfWork = unitOfWork;
            _webHostEnvironment = webHostEnvironment;
        }
        public IActionResult Index()
        {
            return View();
        }
        public IActionResult Upsert(int? id)
        {
            ProductVM productVM = new ProductVM()     //3 things
            {
                Product =new Product(),        //1 Property
                CategoryList = _UnitOfWork.Category.GetAll().Select(cl => new SelectListItem()    //2 Property
                {
                    Text = cl.Name,
                    Value = cl.ID.ToString()
                }),
                CoverTypeList = _UnitOfWork.CoverType.GetAll().Select(ct => new SelectListItem()     //3 Property
                {
                    Text = ct.Name,
                    Value = ct.Id.ToString()

                })
            };
            if (id == null) return View(productVM);
            productVM.Product = _UnitOfWork.Product.Get(id.GetValueOrDefault());
            return View(productVM);

        }

        [HttpPost]
        [AutoValidateAntiforgeryToken]
        public IActionResult Upsert(ProductVM productVM)
        {
            if (ModelState.IsValid)
            {
                var webRootPath = _webHostEnvironment.WebRootPath;    //pick path til wwwrot
                var files = HttpContext.Request.Form.Files;         //it will excess file upload control via id
                if (files.Count() > 0)         //if image is selected
                {
                    var fileName = Guid.NewGuid().ToString();      //Unique name
                    var extension = Path.GetExtension(files[0].FileName);     //Pick Extention
                    var uploads = Path.Combine(webRootPath, @"images\products");  //Set Path

                    //Edit Code
                    if (productVM.Product.Id != 0)  //if image is already in db
                    {
                        var imageExists = _UnitOfWork.Product.Get(productVM.Product.Id).ImageUrl; //find image by id
                        productVM.Product.ImageUrl = imageExists;
                    }
                    if (productVM.Product.ImageUrl != null)  //if we want to update image but image is already in db
                    {
                        var imagePath = Path.Combine(webRootPath, productVM.Product.ImageUrl.TrimStart('\\'));    //Pick image Path

                        if (System.IO.File.Exists(imagePath))  //Checking that file is exist or not
                        {
                            System.IO.File.Delete(imagePath);   //Delete image path
                        }
                    }
                    //It will do all work for image(create)
                    using (var filestream = new FileStream(Path.Combine(uploads,  //read and create file, in uploads we have path
                        fileName+extension),FileMode.Create))
                    {
                        files[0].CopyTo(filestream);
                    }
                    productVM.Product.ImageUrl = @"\images\products\" + fileName + extension;
                }
                else
                {
                    //if we want to edit but not image 
                    if (productVM.Product.Id != 0)// 
                    {
                        var imageExists = _UnitOfWork.Product.Get(productVM.Product.Id).ImageUrl;
                        productVM.Product.ImageUrl = imageExists;
                    }
                }
                if (productVM.Product.Id == 0)
                    _UnitOfWork.Product.Add(productVM.Product);
                else
                    _UnitOfWork.Product.Update(productVM.Product);
                _UnitOfWork.Save();
                return RedirectToAction(nameof(Index));
            }
            else
            {
                productVM = new ProductVM()
                {
                    Product = new Product(),
                    CategoryList = _UnitOfWork.Category.GetAll().Select(cl => new SelectListItem()
                    {
                        Text = cl.Name,
                        Value = cl.ID.ToString(),
                    }),
                    CoverTypeList = _UnitOfWork.CoverType.GetAll().Select(ct => new SelectListItem()
                    {
                        Text = ct.Name,
                        Value = ct.Id.ToString()
                    })
                };
                if(productVM.Product.Id!=0)
                {
                    productVM.Product = _UnitOfWork.Product.Get(productVM.Product.Id);
                }
                return View(productVM);
            }
        }   


        #region APIs
        [HttpGet]
        public IActionResult GetAll()
        {
            
            return Json(new { data = _UnitOfWork.Product.GetAll(includeProperties: "Category,CoverType") });
        }

        [HttpDelete]
        public IActionResult Delete(int id)
        {
            var productInDb = _UnitOfWork.Product.Get(id);
            if (productInDb == null) return Json(new { success = false, message = "Something went wrong!!!" });
            else
                //Image delete
                if (productInDb.ImageUrl != null)  //if we want to update image but image is already in db
            {
                var webRootPath = _webHostEnvironment.WebRootPath;
                var imagePath = Path.Combine(webRootPath, productInDb.ImageUrl.TrimStart('\\'));    //Pick image Path

                if (System.IO.File.Exists(imagePath))  //Checking that file is exist or not
                {
                    System.IO.File.Delete(imagePath);   //Delete image path
                }
            }
            _UnitOfWork.Product.Remove(productInDb);                           
               _UnitOfWork.Save();
              return Json(new { success = true, message = "Data deleted successfully" });
            
            
        }
        #endregion
    }
}
