using BookShoppingProject_1.DataAccess.Repository.IRepository;
using BookShoppingProject_1.Models;
using BookShoppingProject_1.Models.ViewModels;
using BookShoppingProject_1.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Stripe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace BookShoppingProject_1.Areas.Customer.Controllers
{[Area("Customer")]
    [Authorize]
    public class CartController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private static bool isEmailConfirm = false;
        private readonly IEmailSender _emailSender;
        private readonly UserManager<IdentityUser> _userManager;
        public CartController(IUnitOfWork unitOfWork,
            IEmailSender emailSender,UserManager<IdentityUser> userManager )
        {
            _unitOfWork = unitOfWork;
            _emailSender = emailSender;
            _userManager = userManager;
        }
        [BindProperty]
        public ShoppingCartVM ShoppingCartVM { get; set; }
       
        public IActionResult Index()
        {        //here we have to diplay cart info of user
            var claimIdentity = (ClaimsIdentity)User.Identity;   //Which user logged in
            var claim = claimIdentity.FindFirst(ClaimTypes.NameIdentifier);  //
            if (claim == null)
            {
                ShoppingCartVM = new ShoppingCartVM()
                {
                    ListCart = new List<ShoppingCart>()
                };
                return View(ShoppingCartVM);

            }
            ShoppingCartVM = new ShoppingCartVM()
            {
                OrderHeader = new OrderHeader(),
                ListCart = _unitOfWork.ShoppingCart.GetAll(sp => sp.ApplicationUserId ==  //pick cart details of user i.e;logged in
                claim.Value, includeProperties: "Product")
            };
            
            /*****/

            ShoppingCartVM.OrderHeader.OrderTotal = 0;   //Order toata set=0
            ShoppingCartVM.OrderHeader.ApplicationUser = _unitOfWork.ApplicationUser.FirstOrDefault  //pick detail of user that is logged in
                (u => u.Id == claim.Value, includeProperties: "Company");

            foreach (var list in ShoppingCartVM.ListCart)   //loop is for price, price not collumn of table
            {
                list.Price = SD.GetPriceBasedOnQuantity(list.Count, list.Product.Price,  //grand total price
                    list.Product.Price50, list.Product.Price100);
                ShoppingCartVM.OrderHeader.OrderTotal += (list.Count * list.Price);

                if (list.Product.Description.Length > 100)          //if description is more than 100 char then ...
                {
                    list.Product.Description = list.Product.Description.Substring(0, 99) + "...";

                }
            }

            //**********
            if (!isEmailConfirm)
            {
                ViewBag.EmailMessage = "Email has been sent Kindly verify Your email!!!";
                ViewBag.EmailCSS = "text-success";
                isEmailConfirm = false;
            }
            else
            {
                ViewBag.EmailMessage = "Email Must Be Confirm for authorize Customer!!!";
                ViewBag.EmailCSS = "text-danger";
            }

            return View(ShoppingCartVM);            
        }

        [HttpPost]
        [ActionName("Index")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> IndexPost()
        {
            var claimIdentity = (ClaimsIdentity)User.Identity;   //Which user logged in
            var claims = claimIdentity.FindFirst(ClaimTypes.NameIdentifier);
            var user = _unitOfWork.ApplicationUser.FirstOrDefault(u => u.Id == claims.Value);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Email is empty");
            }
            else
            {
                //Email
                var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                var callbackUrl = Url.Page(
                    "/Account/ConfirmEmail",
                    pageHandler: null,
                    values: new { area = "Identity", userId = user.Id, code = code },
                    protocol: Request.Scheme);

                await _emailSender.SendEmailAsync(user.Email, "Confirm your email",
                    $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");
            }
            isEmailConfirm = true;
            return RedirectToAction(nameof(Index));
        }

        public IActionResult plus(int cartId)
        {
            var cart = _unitOfWork.ShoppingCart.FirstOrDefault(sc => sc.Id == cartId);
            cart.Count += 1;
            _unitOfWork.Save();
            return RedirectToAction(nameof(Index));
        }
        public IActionResult minus(int cartId)
        {
            var cart = _unitOfWork.ShoppingCart.FirstOrDefault(sc => sc.Id == cartId);
            if (cart.Count == 1)
            {
                cart.Count = 1;
            }
            else
            {
                cart.Count -= 1;
            }
            _unitOfWork.Save();
            return RedirectToAction(nameof(Index));
        }
        public IActionResult delete(int cartId)
        {
            var cart = _unitOfWork.ShoppingCart.FirstOrDefault(sc => sc.Id == cartId);
            _unitOfWork.ShoppingCart.Remove(cart);
            _unitOfWork.Save();

            //Session
            var claimIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimIdentity.FindFirst(ClaimTypes.NameIdentifier);
            if (claim != null)
            {
                var count = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == claim.Value).ToList().Count;
                HttpContext.Session.SetInt32(SD.Ss_CartSessionCount, count);
            }
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Summary()
        {
            var claimIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimIdentity.FindFirst(ClaimTypes.NameIdentifier);

            ShoppingCartVM = new ShoppingCartVM()
            {
                OrderHeader = new OrderHeader(),
                ListCart = _unitOfWork.ShoppingCart.GetAll(sc => sc.ApplicationUserId == claim.Value, includeProperties: "Product")
            };

            ShoppingCartVM.OrderHeader.ApplicationUser = _unitOfWork.ApplicationUser.FirstOrDefault
                (u => u.Id == claim.Value, includeProperties: "Company");

            foreach (var list in ShoppingCartVM.ListCart)
            {
                list.Price = SD.GetPriceBasedOnQuantity(list.Count, list.Product.Price, list.Product.Price50,
                list.Product.Price100);

                ShoppingCartVM.OrderHeader.OrderTotal += (list.Price * list.Count);
                list.Product.Description = SD.ConvertToRawHtml(list.Product.Description);
            }
            ShoppingCartVM.OrderHeader.Name = ShoppingCartVM.OrderHeader.ApplicationUser.Name;
            ShoppingCartVM.OrderHeader.PhoneNumber = ShoppingCartVM.OrderHeader.ApplicationUser.PhoneNumber;
            ShoppingCartVM.OrderHeader.StreetAddress = ShoppingCartVM.OrderHeader.ApplicationUser.StreetAddress;
            ShoppingCartVM.OrderHeader.City = ShoppingCartVM.OrderHeader.ApplicationUser.City;
            ShoppingCartVM.OrderHeader.State = ShoppingCartVM.OrderHeader.ApplicationUser.State;
            ShoppingCartVM.OrderHeader.PostalCode = ShoppingCartVM.OrderHeader.ApplicationUser.PostalCode;

            return View(ShoppingCartVM);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("Summary")]
        public IActionResult SummaryPost(string StripeToken)
        {
            var claimIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimIdentity.FindFirst(ClaimTypes.NameIdentifier);

            //here we put detail of user who logged in in th orderheader
            ShoppingCartVM.OrderHeader.ApplicationUser = _unitOfWork.ApplicationUser.FirstOrDefault
                (u => u.Id == claim.Value, includeProperties: "Company");

            //Now we put details of item that cx want to purchase in listcart
            ShoppingCartVM.ListCart = _unitOfWork.ShoppingCart.GetAll
                (sc => sc.ApplicationUserId == claim.Value, includeProperties: "Product");

            //here we put payment status Pending in orderheader
            ShoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusPending;

            //here we put order status Pending in orderheader
            ShoppingCartVM.OrderHeader.OrderStatus = SD.OrderStatusPending;

            //puting order date
            ShoppingCartVM.OrderHeader.OrderDate = DateTime.Now;

            //here we put userid of loggedin user in orderheader
            ShoppingCartVM.OrderHeader.ApplicationUserId = claim.Value;

            //here we add all details in order header(Save)
            _unitOfWork.OrderHeader.Add(ShoppingCartVM.OrderHeader);
            _unitOfWork.Save();

            //loop for price in listcart
            foreach (var list in ShoppingCartVM.ListCart)
            {
                list.Price = SD.GetPriceBasedOnQuantity(list.Count, list.Product.Price, list.Product.Price50, list.Product.Price100);
                OrderDetail orderDetail = new OrderDetail()
                {
                    ProductId = list.ProductId,
                    OrderHeaderId = ShoppingCartVM.OrderHeader.Id,
                    Price = list.Price,
                    Count = list.Count
                };
                //when loop will run and items add, total will change
                ShoppingCartVM.OrderHeader.OrderTotal += (list.Price * list.Count);

                _unitOfWork.OrderDetail.Add(orderDetail);
                _unitOfWork.Save();

                //remove data from shopping cart
                _unitOfWork.ShoppingCart.RemoveRange(ShoppingCartVM.ListCart);
                _unitOfWork.Save();


                var covertypeId = 8;
                var categorytypeId = 17;

              var result=  _unitOfWork.Product.GetAll().Where(x => x.CoverTypeId == covertypeId && x.CategoryId == categorytypeId).ToList();


                //SESSION SHOW=0
                HttpContext.Session.SetInt32(SD.Ss_CartSessionCount, 0);
                #region Stripe
                if (StripeToken == null)
                {
                    ShoppingCartVM.OrderHeader.PaymentDueDate = DateTime.Now.AddDays(30);
                    ShoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusDelayPayment;
                    ShoppingCartVM.OrderHeader.OrderStatus = SD.OrderStatusApproved;
                }
                else
                {
                    //paymentProcess
                    var options = new ChargeCreateOptions()
                    {
                        Amount = Convert.ToInt32(ShoppingCartVM.OrderHeader.OrderTotal),
                        Currency = "usd",
                        Description = "orderId :" + ShoppingCartVM.OrderHeader.Id,
                        Source = StripeToken
                    };
                    //Payment
                    var service = new ChargeService();
                    Charge charge = service.Create(options);
                    if (charge.BalanceTransactionId == null)
                        ShoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusRejected;
                    else
                    {
                        ShoppingCartVM.OrderHeader.TransactionId = charge.BalanceTransactionId;
                    }

                    if (charge.Status.ToLower() == "succeeded")
                    {
                        ShoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusApproved;
                        ShoppingCartVM.OrderHeader.OrderStatus = SD.OrderStatusApproved;
                        ShoppingCartVM.OrderHeader.OrderDate = DateTime.Now;
                    }
                }
                _unitOfWork.Save();
                #endregion
            }
            return RedirectToAction("OrderConfirmation", "Cart", new { id = ShoppingCartVM.OrderHeader.Id });
        }

        public IActionResult OrderConfirmation(int id)
        {
            return View();
        }
    }
}
