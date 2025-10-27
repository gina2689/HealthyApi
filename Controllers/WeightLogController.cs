﻿using HealthyApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HealthyApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WeightLogController : ControllerBase
    {
        private readonly DataContext _context;

        public WeightLogController(DataContext context)
        {
            _context = context;
        }

        // ===================== ⑥ 添加体重记录 =====================
        [HttpPost("add")]
        public async Task<IActionResult> AddWeightLog([FromBody] WeightLog log)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // 确保日期有值
            log.Date = log.Date == default ? DateTime.Now : log.Date;

            // 获取用户信息（计算体脂率要用年龄、性别、身高）
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == log.UserId);
            if (user == null)
                return NotFound("找不到对应的用户");

            // 计算 BMI
            if (user.Height.HasValue && user.Height > 0)
            {
                double heightInMeter = user.Height.Value / 100.0; // cm → m
                double bmi = log.Weight / (heightInMeter * heightInMeter);

                // 计算体脂率
                double bodyFat;
                if (user.Gender == "M")
                    bodyFat = 1.2 * bmi + 0.23 * (user.Age ?? 25) - 5.4 - 10.8 * 1;
                else
                    bodyFat = 1.2 * bmi + 0.23 * (user.Age ?? 25) - 5.4;

                log.BodyFat = Math.Round(bodyFat, 2); // 保留两位小数
            }

            _context.WeightLogs.Add(log);
            await _context.SaveChangesAsync();

            return Ok(); // HTTP 200，无返回体
        }

        // ===================== ⑦ 获取体重记录（支持 mode） =====================
        [HttpGet("records")]
        public async Task<IActionResult> GetWeightLogs(
            [FromQuery] int UserId,
            [FromQuery] string mode = "all",
            [FromQuery] DateTime? start = null,
            [FromQuery] DateTime? end = null)
        {
            var query = _context.WeightLogs.Where(w => w.UserId == UserId);

            if (mode == "week")
                query = query.Where(w => w.Date >= DateTime.Now.AddDays(-7));
            else if (mode == "month")
                query = query.Where(w => w.Date >= DateTime.Now.AddMonths(-1));
            else if (start.HasValue && end.HasValue)
                query = query.Where(w => w.Date >= start && w.Date <= end);

            var logs = await query
                .OrderByDescending(w => w.Date)
                .Select(w => new
                {
                    logId = w.LogId,
                    date = w.Date.ToString("yyyy-MM-dd"),
                    timeOfDay = w.TimeOfDay,
                    weight = w.Weight,
                    bodyFat = w.BodyFat
                })
                .ToListAsync();

            return Ok(logs);
        }

        // ===================== ⑧ 删除体重记录 =====================
        [HttpPost("delete")]
        public async Task<IActionResult> DeleteWeightLog([FromBody] DeleteWeightRequest req)
        {
            var log = await _context.WeightLogs
                .FirstOrDefaultAsync(w => w.LogId == req.LogId && w.UserId == req.UserId);

            if (log == null)
                return NotFound("记录不存在");

            _context.WeightLogs.Remove(log);
            await _context.SaveChangesAsync();

            return Ok(); // HTTP200，无内容
        }

        // DTO: 删除请求格式
        public class DeleteWeightRequest
        {
            public int UserId { get; set; }
            public int LogId { get; set; }
        }
    }
}
