using BookShoppingProject_1.DataAccess.Repository.IRepository;
using BookShoppingProject_1.Models;
using BookShoppingProject_1.Models.ViewModels;
using BookShoppingProject_1.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace BookShoppingProject_1.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IUnitOfWork _unitOfWork;

        public HomeController(ILogger<HomeController> logger, IUnitOfWork unitOfWork)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            //Pick looged in user id
            var claimIdentity = (ClaimsIdentity)User.Identity;

            //user details
            var claim = claimIdentity.FindFirst(ClaimTypes.NameIdentifier);

            //if user is logges in or not because it will excess without login
            if (claim != null)
            {
                var count = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == claim.Value).
                    ToList().Count();

                HttpContext.Session.SetInt32(SD.Ss_CartSessionCount, count);
            }

            ProductVM productVM = new ProductVM()     //3 things
            {
                Product = new Product(),        //1 Property
                CategoryList = _unitOfWork.Category.GetAll().Select(cl => new SelectListItem()    //2 Property
                {
                    Text = cl.Name,
                    Value = cl.ID.ToString()
                }),
                CoverTypeList = _unitOfWork.CoverType.GetAll().Select(ct => new SelectListItem()     //3 Property
                {
                    Text = ct.Name,
                    Value = ct.Id.ToString()

                }),
                 ProductList = _unitOfWork.Product.GetAll(includeProperties: "Category,CoverType")

        };
            return View(productVM);
        }
        public IActionResult Details(int id)
        {
            var productInDb = _unitOfWork.Product.FirstOrDefault(p => p.Id == id,includeProperties:"Category,CoverType");
            if (productInDb == null) return NotFound();
            //if not null, we add product in shoppingCart

            var shoppingCart = new ShoppingCart()
            {
                Product = productInDb,
                ProductId = productInDb.Id
            };
            //Session

            //Pick looged in user id
            var claimIdentity = (ClaimsIdentity)User.Identity;

            //user details
            var claim = claimIdentity.FindFirst(ClaimTypes.NameIdentifier);

            //if user is logges in or not because it will excess without login
            if (claim != null)
            {
                var count = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == claim.Value).
                    ToList().Count();

                HttpContext.Session.SetInt32(SD.Ss_CartSessionCount, count);
            }
            return View(shoppingCart);
        }

        [HttpPost]
        [AutoValidateAntiforgeryToken]
        [Authorize]
        public IActionResult Details(ShoppingCart shoppingCart)
        {
            shoppingCart.Id = 0;  //byDefault it pick product id 
            if (ModelState.IsValid)
            {
                //Which user is logged in(User id)
                var claimIdentity = (ClaimsIdentity)User.Identity;

                //(details of user)
                var claim = claimIdentity.FindFirst(ClaimTypes.NameIdentifier);

                //logged in User id store
                shoppingCart.ApplicationUserId = claim.Value;

                //Increase count of product if already added in cart
                var shoppingCartFromDb = _unitOfWork.ShoppingCart.FirstOrDefault
                    (u => u.ApplicationUserId == claim.Value && u.ProductId == shoppingCart.ProductId);
                
                if (shoppingCartFromDb == null)
                {
                    //Add
                    _unitOfWork.ShoppingCart.Add(shoppingCart);
                }
                else
                {
                    //Update
                    shoppingCartFromDb.Count += shoppingCart.Count;
                }
                _unitOfWork.Save();
                return RedirectToAction(nameof(Index));
            }
            else
            {
                var productInDb = _unitOfWork.Product.FirstOrDefault(p => p.Id == shoppingCart.ProductId, includeProperties: "Category,CoverType");
                if (productInDb == null) return NotFound();
                //if not null, we add product in shoppingCart

                var shoppingCartEdit = new ShoppingCart()
                {
                    Product = productInDb,
                    ProductId = productInDb.Id
                };
                return View(shoppingCartEdit);
            }
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
