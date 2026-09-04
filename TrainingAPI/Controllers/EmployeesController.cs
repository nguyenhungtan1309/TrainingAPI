using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrainingAPI.DTOs;
using TrainingAPI.Models;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace TrainingAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmployeesController : ControllerBase
    {
        private readonly CompanyContext _context;
        private readonly IDistributedCache _cache;
        public EmployeesController(CompanyContext context, IDistributedCache cache)
        {
            _context = context;
            _cache = cache;
        }

        // GET: api/Employees
        [HttpGet]
        [EnableQuery]
        public async Task<ActionResult<IEnumerable<Employee>>> GetEmployees()
        {
            string cacheKey = "employeeList";

            var cachedData = await _cache.GetStringAsync(cacheKey);

            if (!string.IsNullOrEmpty(cachedData))
            {
                var employees = JsonSerializer.Deserialize<List<Employee>>(cachedData);
                return Ok(employees.AsQueryable());
            }

            var employeesFromDb = await _context.Employees.ToListAsync();

            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
            };

            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(employeesFromDb), cacheOptions);

            return Ok(employeesFromDb.AsQueryable());
        }

        // POST: api/Employees
        [HttpPost]
        public async Task<ActionResult<EmployeeResponseDTO>> PostEmployee(EmployeeCreateDTO dto)
        {
            bool isEmailExist = await _context.Employees.AnyAsync(e => e.Email == dto.Email);
            if (isEmailExist)
            {
                return BadRequest("Email này đã tồn tại trong hệ thống. Vui lòng sử dụng email khác.");
            }

            var employee = new Employee
            {
                Name = dto.Name,
                Email = dto.Email,
                Position = dto.Position,
                Salary = dto.Salary
            };

            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();

            await _cache.RemoveAsync("employeeList");

            var responseDto = new EmployeeResponseDTO
            {
                Id = employee.Id,
                Name = employee.Name,
                Email = employee.Email,
                Position = employee.Position
            };

            return CreatedAtAction(nameof(GetEmployees), new { id = employee.Id }, responseDto);
        }

        // PUT: api/Employees/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutEmployee(int id, EmployeeCreateDTO dto)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null) return NotFound();

            employee.Name = dto.Name;
            employee.Email = dto.Email;
            employee.Position = dto.Position;
            employee.Salary = dto.Salary;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/Employees/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEmployee(int id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null) return NotFound();

            _context.Employees.Remove(employee);
            await _context.SaveChangesAsync();

            await _cache.RemoveAsync("employeeList");

            return NoContent();
        }
    }
}