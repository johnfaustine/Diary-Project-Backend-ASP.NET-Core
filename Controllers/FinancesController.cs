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
using DiaryProject.Services;
using System.Security.Claims;

namespace DiaryProject.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class FinancesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IUriService _uriService;

        public FinancesController(ApplicationDbContext context, IUriService uriService)
        {
            _context = context;
            _uriService = uriService;
        }

        // GET: api/Finances
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Finance>>> GetFinances()
        {
            var refreshToken = Request.Cookies["refreshToken"];
            if (refreshToken == null)
                return BadRequest("No refresh token found");
            var userId = User.FindFirst(ClaimTypes.NameIdentifier).Value;

            return await _context.Finances.Where(f => f.UserId == userId).ToListAsync();
        }


        ////GET: api/Finances/Search?financeName
        //[HttpGet("Search")]
        //public async Task<ActionResult<IEnumerable<Finance>>> searchFinance([FromQuery] string financeName)
        //{
        //    var refreshToken = Request.Cookies["refreshToken"];
        //    if (refreshToken == null)
        //        return BadRequest("No refresh token found");
        //    var userId = User.FindFirst(ClaimTypes.NameIdentifier).Value;

        //    return await _context.Finances.Where(f => f.UserId == userId && f.FinanceName.Contains(financeName)).ToListAsync();

        //}

        //GET: api/Finances/filter?searchFood=&searchDate=&minCalorie=&maxCalorie=&page=&take=&sortOrder=
        [HttpGet("Filter")]
        public async Task<ActionResult<IEnumerable<Finance>>> SearchFinance(
            [FromQuery] string searchFinance, 
            DateTime? searchDate, 
            Decimal? minIncome, 
            Decimal? maxIncome, 
            Decimal? minExpense, 
            Decimal? maxExpense, 
            int page, 
            int take, 
            string sortOrder)
        {
            var refreshToken = Request.Cookies["refreshToken"];
            if (refreshToken == null)
                return BadRequest("No refresh token found");
            var userId = User.FindFirst(ClaimTypes.NameIdentifier).Value;

            //DateTime date;
            //bool haveDate = !string.IsNullOrEmpty(searchDate) && DateTime.TryParse(searchDate, out date);


            IQueryable<Finance> query = _context.Finances.Where(f => f.UserId == userId);

            switch (sortOrder)
            {
                case "date_asc":
                    query = query.OrderBy(f => f.FinanceDateTime);
                    break;
                case "date_desc":
                    query = query.OrderByDescending(f => f.FinanceDateTime);
                    break;
                case "name_asc":
                    query = query.OrderBy(f => f.FinanceName);
                    break;
                case "name_desc":
                    query = query.OrderByDescending(f => f.FinanceName);
                    break;
                case "income_asc":
                    query = query.OrderBy(f => f.FinanceIncome);
                    break;
                case "income_desc":
                    query = query.OrderByDescending(f => f.FinanceIncome);
                    break;
                case "expense_asc":
                    query = query.OrderBy(f => f.FinanceExpense);
                    break;
                case "expense_desc":
                    query = query.OrderByDescending(f => f.FinanceExpense);
                    break;
                default:
                    query = query.OrderByDescending(f => f.FinanceDateTime);
                    break;
            }
            if (minIncome.HasValue)
                query = query.Where(f => f.FinanceIncome >= minIncome);
            if (maxIncome.HasValue)
                query = query.Where(f => f.FinanceIncome <= maxIncome);
            if (minExpense.HasValue)
                query = query.Where(f => f.FinanceExpense >= minExpense);
            if (maxExpense.HasValue)
                query = query.Where(f => f.FinanceExpense <= maxExpense);

            if (!string.IsNullOrEmpty(searchFinance))
            {
                query = query.Where(f => f.FinanceName.Contains(searchFinance));
            }

            if (searchDate.HasValue)
            {
                //date = DateTime.Parse(searchDate);
                query = query.Where(f => f.FinanceDateTime.Date == searchDate);
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

        // GET: api/Finances/5
        //[HttpGet("{id}")]
        //public async Task<ActionResult<Finance>> GetFinance(int id)
        //{
        //    var refreshToken = Request.Cookies["refreshToken"];
        //    if (refreshToken == null)
        //        return BadRequest("No refresh token found");
        //    var finance = await _context.Finances.FindAsync(id);

        //    if (finance == null)
        //    {
        //        return NotFound();
        //    }

        //    return finance;
        //}

        // GET: api/Finances/filterDate
        [HttpGet("filterDate")]
        public async Task<ActionResult<IEnumerable<Finance>>> Get([FromQuery] DateTime date)
        {
            var refreshToken = Request.Cookies["refreshToken"];
            if (refreshToken == null)
                return BadRequest("No refresh token found");
            var userId = User.FindFirst(ClaimTypes.NameIdentifier).Value;
            return await _context.Finances.Where(f => f.UserId == userId && f.FinanceDateTime.Date == date).ToListAsync();

        }

        // GET: api/Finances/filterMonthYear?month=?year=
        [HttpGet("filterMonthYear")]
        public async Task<ActionResult<IEnumerable<Finance>>> GetFinancebyMonthYear([FromQuery] int month, int year)
        {
            var refreshToken = Request.Cookies["refreshToken"];
            if (refreshToken == null)
                return BadRequest("No refresh token found");
            var userId = User.FindFirst(ClaimTypes.NameIdentifier).Value;
            return await _context.Finances.Where(f => f.UserId == userId && f.FinanceDateTime.Date.Month == month && f.FinanceDateTime.Date.Year == year).ToListAsync();

        }

        // GET: api/Finances/filterYear?year=
        [HttpGet("filterYear")]
        public async Task<ActionResult<IEnumerable<Finance>>> GetFinancebyYear([FromQuery] int year)
        {
            var refreshToken = Request.Cookies["refreshToken"];
            if (refreshToken == null)
                return BadRequest("No refresh token found");
            var userId = User.FindFirst(ClaimTypes.NameIdentifier).Value;
            return await _context.Finances.Where(f => f.UserId == userId && f.FinanceDateTime.Date.Year == year).ToListAsync();

        }

        // PUT: api/Finances/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for
        // more details, see https://go.microsoft.com/fwlink/?linkid=2123754.
        [HttpPut("{id}")]
        public async Task<IActionResult> PutFinance(int id, Finance finance)
        {
            var refreshToken = Request.Cookies["refreshToken"];
            if (refreshToken == null)
                return BadRequest("No refresh token found");
            if (id != finance.FinanceId)
            {
                return BadRequest();
            }

            if(finance.UserId != User.FindFirst(ClaimTypes.NameIdentifier).Value)
            {
                return BadRequest("Unauthorized");
            }

            _context.Entry(finance).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!FinanceExists(id))
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

        // POST: api/Finances
        // To protect from overposting attacks, enable the specific properties you want to bind to, for
        // more details, see https://go.microsoft.com/fwlink/?linkid=2123754.
        [HttpPost]
        public async Task<ActionResult<Finance>> PostFinance(Finance finance)
        {
            var refreshToken = Request.Cookies["refreshToken"];
            if (refreshToken == null)
                return BadRequest("No refresh token found");
            var userId = User.FindFirst(ClaimTypes.NameIdentifier).Value;
            finance.UserId = userId;
            _context.Finances.Add(finance);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetFinance", new { id = finance.FinanceId }, finance);
        }

        // DELETE: api/Finances/5
        [HttpDelete("{id}")]
        public async Task<ActionResult<Finance>> DeleteFinance(int id)
        {
            var refreshToken = Request.Cookies["refreshToken"];
            if (refreshToken == null)
                return BadRequest("No refresh token found");
            var finance = await _context.Finances.FindAsync(id);
            if (finance == null)
            {
                return NotFound();
            }

            if (finance.UserId != User.FindFirst(ClaimTypes.NameIdentifier).Value)
            {
                return BadRequest("Unauthorized");
            }

            _context.Finances.Remove(finance);
            await _context.SaveChangesAsync();

            return finance;
        }

        private bool FinanceExists(int id)
        {
            return _context.Finances.Any(e => e.FinanceId == id);
        }
    }
}
