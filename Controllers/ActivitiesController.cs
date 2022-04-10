using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DiaryProject.Data;
using DiaryProject.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using DiaryProject.Services;
using DiaryProject.Helpers;
using Microsoft.AspNetCore.JsonPatch;

namespace DiaryProject.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ActivitiesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IUriService _uriService;

        public ActivitiesController(ApplicationDbContext context, IUriService uriService)
        {
            _context = context;
            _uriService = uriService;
        }

        //GET: api/Activities
       [HttpGet]
        public async Task<ActionResult<IEnumerable<Activity>>> GetActivities([FromQuery] PaginationFilter filter)
        {
            var refreshToken = Request.Cookies["refreshToken"];
            if (refreshToken == null)
                return BadRequest("No refresh token found");
            var userId = User.FindFirst(ClaimTypes.NameIdentifier).Value;


            //var route = Request.Path.Value;


            return await _context.Activities.Where(act => act.UserId == userId).ToListAsync();


            //var validFilter = new PaginationFilter(filter.PageNumber, filter.PageSize);
            //var pagedData = await _context.Activities.Where(act => act.UserId == userId)
            //   .Skip((validFilter.PageNumber - 1) * validFilter.PageSize)
            //   .Take(validFilter.PageSize)
            //   .ToListAsync();
            //var totalRecords = await _context.Activities.Where(act => act.UserId == userId).CountAsync();
            //var pagedResponse = PaginationHelper.CreatePagedReponse<Activity>(pagedData, validFilter, totalRecords, _uriService, route);
            //return Ok(pagedResponse);


        }

        ////GET: api/Activities/Search?activityName
        //[HttpGet("Search")]
        //public async Task<ActionResult<IEnumerable<Activity>>> searchActivity([FromQuery] string activityName)
        //{
        //    var refreshToken = Request.Cookies["refreshToken"];
        //    if (refreshToken == null)
        //        return BadRequest("No refresh token found");
        //    var userId = User.FindFirst(ClaimTypes.NameIdentifier).Value;

        //    if (!string.IsNullOrEmpty(activityName))
        //    {
        //        return await _context.Activities.Where(act => act.UserId == userId && act.ActivityName.Contains(activityName)).ToListAsync();
        //    }
        //    return Ok("");

        //}

        //GET: api/Activities/filter?searchActivity=&searchDate=&page=&take=&sortOrder=
        [HttpGet("Filter")]
        public async Task<ActionResult<IEnumerable<Activity>>> SearchActivity([FromQuery] string searchActivity, DateTime? searchDate, int page, int take, string sortOrder)
        {
            var refreshToken = Request.Cookies["refreshToken"];
            if (refreshToken == null)
                return BadRequest("No refresh token found");
            var userId = User.FindFirst(ClaimTypes.NameIdentifier).Value;

            //DateTime date;
            //bool haveDate = !string.IsNullOrEmpty(searchDate) && DateTime.TryParse(searchDate, out date);


            IQueryable<Activity> query = _context.Activities.Where(act => act.UserId == userId);

            switch (sortOrder)
            {
                case "date_asc":
                    query = query.OrderBy(act => act.ActivityDateTime);
                    break;
                case "date_desc":
                    query = query.OrderByDescending(act => act.ActivityDateTime);
                    break;
                case "name_asc":
                    query = query.OrderBy(act => act.ActivityName);
                    break;
                case "name_desc":
                    query = query.OrderByDescending(act => act.ActivityName);
                    break;
                default:
                    query = query.OrderByDescending(act => act.ActivityDateTime);
                    break;
            }

            if (!string.IsNullOrEmpty(searchActivity))
            {
                query = query.Where(act => act.ActivityName.Contains(searchActivity));
            }

            //if (haveDate)
            //{
            //    date = DateTime.Parse(searchDate);
            //    query = query.Where(act => act.ActivityDateTime.Date == date);
            //}

            if (searchDate.HasValue)
            {
                query = query.Where(act => act.ActivityDateTime.Date == searchDate);
            }
            var totalRecords = await query.CountAsync();
            var totalPage = Math.Ceiling((double)totalRecords / take);
            int skip = (page - 1) * take;
            return new ObjectResult(new
            {
                data = query.Skip(skip).Take(take).ToListAsync(),
                totalPage = totalPage
            });
            //return await query.Skip(skip).Take(take).ToListAsync();
        }

        // GET: api/Activities/5
        //[HttpGet("{id}")]
        //public async Task<ActionResult<Activity>> GetActivity(int id)
        //{
        //    var refreshToken = Request.Cookies["refreshToken"];
        //    if (refreshToken == null)
        //        return BadRequest("No refresh token found");
        //    var activity = await _context.Activities.FindAsync(id);

        //    if (activity == null)
        //    {
        //        return NotFound();
        //    }

        //    return activity;
        //    //return Ok(new Response<Activity>(activity));
        //}



        [HttpGet("filterDate")]
        public async Task<ActionResult<IEnumerable<Activity>>> Get([FromQuery] DateTime date)
        {
            var refreshToken = Request.Cookies["refreshToken"];
            if (refreshToken == null)
                return BadRequest("No refresh token found");
            var userId = User.FindFirst(ClaimTypes.NameIdentifier).Value;
            return await _context.Activities.Where(a => a.UserId == userId && a.ActivityDateTime.Date == date).ToListAsync();
      
        }

        // PUT: api/Activities/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for
        // more details, see https://go.microsoft.com/fwlink/?linkid=2123754.
        [HttpPut("{id}")]
        public async Task<IActionResult> PutActivity(int id, Activity activity)
        {
            var refreshToken = Request.Cookies["refreshToken"];
            if (refreshToken == null)
                return BadRequest("No refresh token found");
            if (id != activity.ActivityId)
            {
                return BadRequest();
            }

            if (activity.UserId != User.FindFirst(ClaimTypes.NameIdentifier).Value)
            {
                return BadRequest("Unauthorized");
            }

            _context.Entry(activity).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ActivityExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        [HttpPatch("{id}")]
        public async Task<IActionResult> PatchActivity(int id, JsonPatchDocument<Activity> patchDoc)
        {
            var refreshToken = Request.Cookies["refreshToken"];
            if (refreshToken == null)
                return BadRequest("No refresh token found");
            if (patchDoc == null)
            {
                return BadRequest();
            }

            var activity = await _context.Activities.FirstOrDefaultAsync(x => x.ActivityId == id);
            if (activity == null)
            {
                return NotFound();
            }
            patchDoc.ApplyTo(activity, ModelState);
            var isValid = TryValidateModel(activity);
            if (!isValid)
            {
                return BadRequest(ModelState);
            }

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // POST: api/Activities
        // To protect from overposting attacks, enable the specific properties you want to bind to, for
        // more details, see https://go.microsoft.com/fwlink/?linkid=2123754.
        [HttpPost]
        public async Task<ActionResult<Activity>> PostActivity(Activity activity)
        {
            var refreshToken = Request.Cookies["refreshToken"];
            if (refreshToken == null)
                return BadRequest("No refresh token found");
            var userId = User.FindFirst(ClaimTypes.NameIdentifier).Value;
            activity.UserId = userId;
            //activity.ActivityDateTime = DateTime.Now;
            _context.Activities.Add(activity);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetActivity", new { id = activity.ActivityId }, activity);
        }

        // DELETE: api/Activities/5
        [HttpDelete("{id}")]
        public async Task<ActionResult<Activity>> DeleteActivity(int id)
        {
            var refreshToken = Request.Cookies["refreshToken"];
            if (refreshToken == null)
                return BadRequest("No refresh token found");
            var activity = await _context.Activities.FindAsync(id);
            if (activity == null)
            {
                return NotFound();
            }

            if (activity.UserId != User.FindFirst(ClaimTypes.NameIdentifier).Value)
            {
                return BadRequest("Unauthorized");
            }

            _context.Activities.Remove(activity);
            await _context.SaveChangesAsync();

            return activity;
        }

        private bool ActivityExists(int id)
        {
            return _context.Activities.Any(e => e.ActivityId == id);
        }
    }
}
