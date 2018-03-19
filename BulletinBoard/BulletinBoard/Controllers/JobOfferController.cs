﻿using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using BulletinBoard.Data;
using BulletinBoard.Models;
using BulletinBoard.Models.ErrorViewModels;
using BulletinBoard.Models.JobOfferViewModels;
using BulletinBoard.Services.Abstract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace BulletinBoard.Controllers
{
    [Authorize]
    public class JobOfferController : Controller
    {
        private readonly IJobOfferService _jobOfferService;
        private readonly IJobCategoryService _jobCategoryService;
        private readonly IJobTypeService _jobTypeService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public JobOfferController(
            IJobOfferService jobOfferService,
            IJobCategoryService jobCategoryService,
            IJobTypeService jobTypeService,
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _jobOfferService = jobOfferService;
            _jobCategoryService = jobCategoryService;
            _jobTypeService = jobTypeService;
            
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [AllowAnonymous]
        public IActionResult Error(int? statusCode)
        {
            var vm = new ErrorViewModel
            {
                Response = statusCode?.ToString() ?? "-",
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            };

            return View(vm);
        }

        // GET: JobOffer
        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            var jobOffers = await _jobOfferService.GetAllOffers();
            var vms = Mapper.Map<IList<JobOfferViewModel>>(jobOffers);
            ViewData["JobOfferCount"] = vms.Count;
            
            if (!_signInManager.IsSignedIn(HttpContext.User))
            {
                return View(vms);
            }

            var user = await GetCurrentUser();
            foreach (var offer in vms)
            {
                offer.CanEdit = await _jobOfferService.CanUserEditOffer(user.Id, offer.JobOfferId);
            }

            return View(vms);
        }

        [AllowAnonymous]
        public async Task<IActionResult> Search(string phrase)
        {
            if (string.IsNullOrEmpty(phrase))
            {
                return RedirectToAction(nameof(Index));
            }

            var matchingOffers = await _jobOfferService.GetOffersContainingPhrase(phrase);
            var vms = Mapper.Map<IList<JobOfferViewModel>>(matchingOffers);
            ViewData["JobOfferCount"] = vms.Count;
            ViewData["phrase"] = phrase;

            if (!_signInManager.IsSignedIn(HttpContext.User))
            {
                return View("Index", vms);
            }

            var user = await GetCurrentUser();
            foreach (var offer in vms)
            {
                offer.CanEdit = await _jobOfferService.CanUserEditOffer(user.Id, offer.JobOfferId);
            }
            return View("Index", vms);
        }

        // GET: JobOffer/Popular
        [AllowAnonymous]
        public async Task<IActionResult> Popular()
        {
            var popularJobOffers = await _jobOfferService.GetMostPopularOffers();
            var vms = Mapper.Map<IEnumerable<PopularJobOfferViewModel>>(popularJobOffers);
            return View(vms);
        }

        // GET: JobOffer/Details/5
        [AllowAnonymous]
        public async Task<IActionResult> Details(string id)
        {
            if (id == null)
            {
                return View("NotFound");
            }

            var jobOffer = await _jobOfferService.GetOfferById(id);
            if (jobOffer == null)
            {
                return View("NotFound");
            }

            await _jobOfferService.IncreaseOfferViews(jobOffer);

            var vm = Mapper.Map<DetailsJobOfferViewModel>(jobOffer);
            if (!_signInManager.IsSignedIn(HttpContext.User))
            {
                return View(vm);
            }

            var user = await GetCurrentUser();
            vm.CanEdit = await _jobOfferService.CanUserEditOffer(user.Id, vm.JobOfferId);
            return View(vm);
        }

        // GET: JobOffer/Create
        public async Task<IActionResult> Create()
        {
            var user = GetCurrentUser().Result;
            var viewModel = new CreateJobOfferViewModel
            {
                AuthorId = user.Id,
                JobCategories = await _jobCategoryService.GetAllCategories(),
                JobTypes = await _jobTypeService.GetAllTypes()
            };

            return View(viewModel);
        }

        // POST: JobOffer/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateJobOfferViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.JobCategories = await _jobCategoryService.GetAllCategories();
                model.JobTypes = await _jobTypeService.GetAllTypes();
                return View(model);
            }

            var jobOffer = Mapper.Map<JobOffer>(model);
            var result = await _jobOfferService.Add(jobOffer);
            if (result)
            {
                return RedirectToAction(nameof(Index));
            }

            return View("NotFound");
        }

        // GET: JobOffer/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null)
            {
                return View("NotFound");
            }
            var offer = await _jobOfferService.GetOfferById(id);
            if (offer == null)
            {
                return View("NotFound");
            }

            var user = await GetCurrentUser();
            if (!await _jobOfferService.CanUserEditOffer(user.Id, offer.JobOfferId))
            {
                return View("AccessDenied");
            }

            var vm = Mapper.Map<EditJobOfferViewModel>(offer);
            vm.JobCategories = await _jobCategoryService.GetAllCategories();
            vm.JobTypes = await _jobTypeService.GetAllTypes();
            return View(vm);
        }

        // POST: JobOffer/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditJobOfferViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.JobCategories = await _jobCategoryService.GetAllCategories();
                model.JobTypes = await _jobTypeService.GetAllTypes();
                return View(model);
            }

            var offer = Mapper.Map<JobOffer>(model);
            var result = await _jobOfferService.Edit(offer);
            if (result)
            {
                return RedirectToAction(nameof(Index));
            }

            return View("NotFound");
        }

        // GET: JobOffer/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null)
            {
                return View("NotFound");
            }

            var jobOffer = await _jobOfferService.GetOfferById(id);
            if (jobOffer == null)
            {
                return View("NotFound");
            }

            var viewModel = Mapper.Map<DeleteJobOfferViewModel>(jobOffer);
            return View(viewModel);
        }

        // POST: JobOffer/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(DeleteJobOfferViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            
            var offer = Mapper.Map<JobOffer>(model);
            var result = await _jobOfferService.Delete(offer);
            if (result)
            {
                return RedirectToAction(nameof(Index));
            }

            return View("NotFound");
        }

        private async Task<ApplicationUser> GetCurrentUser()
        {
            return await _userManager.GetUserAsync(User);
        }
    }
}
