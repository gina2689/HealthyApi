using HealthyApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthyApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WaterLogController : ControllerBase
    {
        private readonly DataContext _context;

        public WaterLogController(DataContext context)
        {
            _context = context;
        }

        // ===================== ③ 添加饮水记录 =====================
        // POST: api/WaterLog/add
        [HttpPost("add")]
        public async Task<IActionResult> AddWaterLog([FromBody] WaterLog log)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // 自动填充日期（如果没传）
            if (log.Date == default)
                log.Date = DateTime.Now.Date;

            _context.WaterLogs.Add(log);
            await _context.SaveChangesAsync();

            // 不返回内容，仅HTTP200
            return Ok();
        }

        // ===================== ④ 获取饮水记录 =====================
        // GET: api/WaterLog/records
        [HttpGet("records")]
        public async Task<IActionResult> GetWaterLogs(
            [FromQuery] int userId,
            [FromQuery] string mode,
            [FromQuery] DateTime? date = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            var query = _context.WaterLogs
                .Where(w => w.UserId == userId)
                .AsQueryable();

            // ① today 模式：单天记录
            if (mode == "today" && date.HasValue)
            {
                var records = await query
                    .Where(w => w.Date == date.Value.Date)
                    .OrderBy(w => w.Time)
                    .Select(w => new
                    {
                        id = w.WaterId,
                        time = w.Time,
                        drink = w.WaterType,
                        amount = w.Amount
                    })
                    .ToListAsync();

                var result = new
                {
                    date = date.Value.ToString("yyyy-MM-dd"),
                    records = records
                };

                return Ok(result);
            }

            // ② week / month 模式：日期区间
            if ((mode == "week" || mode == "month") && startDate.HasValue && endDate.HasValue)
            {
                query = query.Where(w => w.Date >= startDate && w.Date <= endDate);
            }

            // ③ all 模式：返回全部记录
            if (mode == "all")
            {
                // 不做筛选
            }

            // 分组返回结果
            var grouped = await query
                .OrderByDescending(w => w.Date)
                .ThenBy(w => w.Time)
                .GroupBy(w => w.Date)
                .Select(g => new
                {
                    date = g.Key.ToString("yyyy-MM-dd"),
                    records = g.Select(w => new
                    {
                        id = w.WaterId,
                        time = w.Time,
                        drink = w.WaterType,
                        amount = w.Amount
                    }).ToList()
                })
                .ToListAsync();

            return Ok(new { data = grouped });
        }

        // ===================== ⑤ 删除饮水记录 =====================
        // POST: api/WaterLog/delete
        [HttpPost("delete")]
        public async Task<IActionResult> DeleteWaterLog([FromBody] DeleteWaterRequest req)
        {
            var record = await _context.WaterLogs
                .FirstOrDefaultAsync(w => w.WaterId == req.RecordId && w.UserId == req.UserId);

            if (record == null)
                return NotFound("记录不存在");

            _context.WaterLogs.Remove(record);
            await _context.SaveChangesAsync();

            return Ok(); // HTTP200，无返回体
        }
    }

    // DTO 删除请求结构
    public class DeleteWaterRequest
    {
        public int UserId { get; set; }
        public int RecordId { get; set; }
    }
}
