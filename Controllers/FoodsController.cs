using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DiaryProject.Data;
using DiaryProject.Models;
using System.Security.Claims;
using DiaryProject.Services;
using Microsoft.AspNetCore.Authorization;

namespace DiaryProject.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class FoodsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IUriService _uriService;

        public FoodsController(ApplicationDbContext context, IUriService uriService)
        {
            _context = context;
            _uriService = uriService;
        }

        // GET: api/Foods
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Food>>> GetFoods()
        {
            var refreshToken = Request.Cookies["refreshToken"];
            if (refreshToken == null)
                return BadRequest("No refresh token found");
            var userId = User.FindFirst(ClaimTypes.NameIdentifier).Value;

            return await _context.Foods.Where(f => f.UserId == userId).ToListAsync();
        }


        ////GET: api/Foods/Search?foodName=foodCalorie
        //[HttpGet("Search")]
        //public async Task<ActionResult<IEnumerable<Food>>> searchFood([FromQuery] string foodName)
        //{
        //    var refreshToken = Request.Cookies["refreshToken"];
        //    if (refreshToken == null)
        //        return BadRequest("No refresh token found");
        //    var userId = User.FindFirst(ClaimTypes.NameIdentifier).Value;

        //    return await _context.Foods.Where(f => f.UserId == userId && f.FoodName.Contains(foodName)).ToListAsync();

        //}

        //GET: api/Foods/filter?searchFood=&searchDate=&minCalorie=&maxCalorie=&page=&take=&sortOrder=
        [HttpGet("Filter")]
        public async Task<ActionResult<IEnumerable<Food>>> SearchFood([FromQuery] string searchFood, DateTime? searchDate, int? minCalorie, int? maxCalorie, int page, int take, string sortOrder)
        {
            var refreshToken = Request.Cookies["refreshToken"];
            if (refreshToken == null)
                return BadRequest("No refresh token found");
            var userId = User.FindFirst(ClaimTypes.NameIdentifier).Value;

            //DateTime date;
            //bool haveDate = !string.IsNullOrEmpty(searchDate) && DateTime.TryParse(searchDate, out date);


            IQueryable<Food> query = _context.Foods.Where(f => f.UserId == userId);

            switch (sortOrder)
            {
                case "date_asc":
                    query = query.OrderBy(f => f.FoodDateTime);
                    break;
                case "date_desc":
                    query = query.OrderByDescending(f => f.FoodDateTime);
                    break;
                case "name_asc":
                    query = query.OrderBy(f => f.FoodName);
                    break;
                case "name_desc":
                    query = query.OrderByDescending(f => f.FoodName);
                    break;
                case "calorie_asc":
                    query = query.OrderBy(f => f.FoodCalorie);
                    break;
                case "calorie_desc":
                    query = query.OrderByDescending(f => f.FoodCalorie);
                    break;
                default:
                    query = query.OrderByDescending(f => f.FoodDateTime);
                    break;
            }
            if (minCalorie.HasValue)
                query = query.Where(f => f.FoodCalorie >= minCalorie);
            if (maxCalorie.HasValue)
                query = query.Where(f => f.FoodCalorie <= maxCalorie);

            if (!string.IsNullOrEmpty(searchFood))
            {
                query = query.Where(f => f.FoodName.Contains(searchFood));
            }

            if (searchDate.HasValue)
            {
                //date = DateTime.Parse(searchDate);
                query = query.Where(f => f.FoodDateTime.Date == searchDate);
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

        // GET: api/Foods/5
        //[HttpGet("{id}")]
        //public async Task<ActionResult<Food>> GetFood(int id)
        //{
        //    var refreshToken = Request.Cookies["refreshToken"];
        //    if (refreshToken == null)
        //        return BadRequest("No refresh token found");
        //    var food = await _context.Foods.FindAsync(id);

        //    if (food == null)
        //    {
        //        return NotFound();
        //    }

        //    return food;
        //}

        // GET: api/Foods/filterDate?date=
        [HttpGet("filterDate")]
        public async Task<ActionResult<IEnumerable<Food>>> Get([FromQuery] DateTime date)
        {
            var refreshToken = Request.Cookies["refreshToken"];
            if (refreshToken == null)
                return BadRequest("No refresh token found");
            var userId = User.FindFirst(ClaimTypes.NameIdentifier).Value;
            return await _context.Foods.Where(f => f.UserId == userId && f.FoodDateTime.Date == date).ToListAsync();

        }

        // GET: api/Foods/filterMonthYear?month=?year=
        [HttpGet("filterMonthYear")]
        public async Task<ActionResult<IEnumerable<Food>>> GetFoodbyMonthYear([FromQuery] int month, int year)
        {
            var refreshToken = Request.Cookies["refreshToken"];
            if (refreshToken == null)
                return BadRequest("No refresh token found");
            var userId = User.FindFirst(ClaimTypes.NameIdentifier).Value;
            return await _context.Foods.Where(f => f.UserId == userId && f.FoodDateTime.Date.Month == month && f.FoodDateTime.Date.Year == year).ToListAsync();

        }

        // GET: api/Foods/filterYear?year=
        [HttpGet("filterYear")]
        public async Task<ActionResult<IEnumerable<Food>>> GetFoodbyYear([FromQuery] int year)
        {
            var refreshToken = Request.Cookies["refreshToken"];
            if (refreshToken == null)
                return BadRequest("No refresh token found");
            var userId = User.FindFirst(ClaimTypes.NameIdentifier).Value;
            return await _context.Foods.Where(f => f.UserId == userId && f.FoodDateTime.Date.Year == year).ToListAsync();

        }

        // PUT: api/Foods/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for
        // more details, see https://go.microsoft.com/fwlink/?linkid=2123754.
        [HttpPut("{id}")]
        public async Task<IActionResult> PutFood(int id, Food food)
        {
            var refreshToken = Request.Cookies["refreshToken"];
            if (refreshToken == null)
                return BadRequest("No refresh token found");
            if (id != food.FoodId)
            {
                return BadRequest();
            }


            if (food.UserId != User.FindFirst(ClaimTypes.NameIdentifier).Value)
            {
                return BadRequest("Unauthorized");
            }

            _context.Entry(food).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!FoodExists(id))
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

        // POST: api/Foods
        // To protect from overposting attacks, enable the specific properties you want to bind to, for
        // more details, see https://go.microsoft.com/fwlink/?linkid=2123754.
        [HttpPost]
        public async Task<ActionResult<Food>> PostFood(Food food)
        {
            var refreshToken = Request.Cookies["refreshToken"];
            if (refreshToken == null)
                return BadRequest("No refresh token found");
            var userId = User.FindFirst(ClaimTypes.NameIdentifier).Value;
            food.UserId = userId;
            _context.Foods.Add(food);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetFood", new { id = food.FoodId }, food);
        }

        // DELETE: api/Foods/5
        [HttpDelete("{id}")]
        public async Task<ActionResult<Food>> DeleteFood(int id)
        {
            var refreshToken = Request.Cookies["refreshToken"];
            if (refreshToken == null)
                return BadRequest("No refresh token found");
            var food = await _context.Foods.FindAsync(id);
            if (food == null)
            {
                return NotFound();
            }

            if (food.UserId != User.FindFirst(ClaimTypes.NameIdentifier).Value)
            {
                return BadRequest("Unauthorized");
            }

            _context.Foods.Remove(food);
            await _context.SaveChangesAsync();

            return food;
        }

        private bool FoodExists(int id)
        {
            return _context.Foods.Any(e => e.FoodId == id);
        }
    }
}
