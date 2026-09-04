using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using TrainingAPI.DTOs;
using TrainingAPI.Hubs;
using TrainingAPI.Models;

namespace TrainingAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmployeesController : ControllerBase
    {
        private readonly CompanyContext _context;
        private readonly IDistributedCache _cache;
        private readonly IHubContext<ChatHub> _hubContext;
        private const string EmployeeCacheKey = "employeeList";

        public EmployeesController(
            CompanyContext context,
            IDistributedCache cache,
            IHubContext<ChatHub> hubContext)
        {
            _context = context;
            _cache = cache;
            _hubContext = hubContext;
        }

        // GET: api/Employees (Hỗ trợ OData $filter, $orderby, $top, $skip + Redis Cache)
        [HttpGet]
        [EnableQuery]
        public async Task<ActionResult<IQueryable<Employee>>> GetEmployees()
        {
            var cachedData = await _cache.GetStringAsync(EmployeeCacheKey);
            if (!string.IsNullOrEmpty(cachedData))
            {
                var cachedEmployees = JsonSerializer.Deserialize<List<Employee>>(cachedData);
                return Ok(cachedEmployees.AsQueryable());
            }

            var employeesFromDb = await _context.Employees.ToListAsync();

            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
            };
            await _cache.SetStringAsync(
                EmployeeCacheKey,
                JsonSerializer.Serialize(employeesFromDb),
                cacheOptions);

            return Ok(employeesFromDb.AsQueryable());
        }

        // GET: api/Employees/5
        [HttpGet("{id}")]
        public async Task<ActionResult<EmployeeResponseDTO>> GetEmployee(int id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null) return NotFound();

            return Ok(new EmployeeResponseDTO
            {
                Id = employee.Id,
                Name = employee.Name,
                Email = employee.Email,
                Position = employee.Position
            });
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

            await _cache.RemoveAsync(EmployeeCacheKey);

            await _hubContext.Clients.All.SendAsync("ReceiveMessage", "System", $"Nhân viên mới '{employee.Name}' vừa được thêm vào hệ thống.");

            var responseDto = new EmployeeResponseDTO
            {
                Id = employee.Id,
                Name = employee.Name,
                Email = employee.Email,
                Position = employee.Position
            };

            return CreatedAtAction(nameof(GetEmployee), new { id = employee.Id }, responseDto);
        }

        // PUT: api/Employees/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutEmployee(int id, EmployeeCreateDTO dto)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null) return NotFound();

            bool isEmailExist = await _context.Employees.AnyAsync(e => e.Email == dto.Email && e.Id != id);
            if (isEmailExist)
            {
                return BadRequest("Email đã được sử dụng bởi nhân viên khác.");
            }

            employee.Name = dto.Name;
            employee.Email = dto.Email;
            employee.Position = dto.Position;
            employee.Salary = dto.Salary;

            await _context.SaveChangesAsync();

            await _cache.RemoveAsync(EmployeeCacheKey);

            await _hubContext.Clients.All.SendAsync("ReceiveMessage", "System", $"Thông tin nhân viên '{employee.Name}' (ID: {id}) đã được cập nhật.");

            return NoContent();
        }

        // DELETE: api/Employees/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEmployee(int id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null) return NotFound();

            string employeeName = employee.Name;
            _context.Employees.Remove(employee);
            await _context.SaveChangesAsync();

            await _cache.RemoveAsync(EmployeeCacheKey);

            await _hubContext.Clients.All.SendAsync("ReceiveMessage", "System", $"Nhân viên '{employeeName}' (ID: {id}) đã bị xóa khỏi hệ thống.");

            return NoContent();
        }
    }
}