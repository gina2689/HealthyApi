using HealthyApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Text;
using System.Text.Json;

namespace HealthyApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FoodRecognitionController : ControllerBase
    {
        private readonly DataContext _context;
        private readonly HttpClient _httpClient;

        public FoodRecognitionController(DataContext context)
        {
            _context = context;
            _httpClient = new HttpClient();
        }

        // ===================== (1) 上传图片并识别 =====================
        [HttpPost("recognize")]
        [Consumes("multipart/form-data")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IActionResult> RecognizeFood([FromForm] IFormFile image, [FromForm] int user_id, [FromForm] string meal_type)
        {
            if (image == null || image.Length == 0)
                return BadRequest("未上传图片");

            try
            {
                // 临时保存图片
                string folder = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
                Directory.CreateDirectory(folder);
                string filePath = Path.Combine(folder, $"{Guid.NewGuid()}.jpg");
                using (var stream = new FileStream(filePath, FileMode.Create))
                    await image.CopyToAsync(stream);

                // 调百度识别
                var foods = await RecognizeFromBaiduAsync(filePath);

                // 如果识别成功则入库
                if (foods.Any())
                {
                    var meal = new MealLog
                    {
                        UserId = user_id,
                        Date = DateTime.Now.Date,
                        MealType = meal_type,
                        Note = "AI识别自动添加"
                    };
                    _context.MealLogs.Add(meal);
                    await _context.SaveChangesAsync();

                    foreach (var f in foods)
                    {
                        _context.MealFoods.Add(new MealFood
                        {
                            MealId = meal.MealId,
                            FoodName = f.NameKr,
                            Amount = 100,
                            Calorie = f.Calories
                        });
                    }
                    await _context.SaveChangesAsync();
                }

                return Ok(new
                {
                    message = "识别成功",
                    items = foods.Select(f => new
                    {
                        cn = f.NameCn,
                        kr = f.NameKr,
                        kcal = f.Calories
                    })
                });
            }
            catch (InvalidOperationException ex)
            {
                // token 或识别失败
                return StatusCode(502, new { message = "百度接口错误", detail = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "服务器错误", detail = ex.Message });
            }
        }

        // ===================== (2) 百度识别核心逻辑 =====================
        private async Task<List<FoodItem>> RecognizeFromBaiduAsync(string imagePath)
        {
            string accessToken = await GetBaiduAccessTokenAsync();

            // 调用菜品识别
            string json = await CallDishApiAsync(accessToken, imagePath);
            using var doc = JsonDocument.Parse(json);
            var resultList = new List<FoodItem>();

            // 如果返回有错误码，直接报错
            if (doc.RootElement.TryGetProperty("error_code", out var err))
            {
                var msg = doc.RootElement.TryGetProperty("error_msg", out var em)
                    ? em.GetString() : "未知错误";
                throw new InvalidOperationException($"dish api error: {err} {msg}");
            }

            // 读取结果
            if (doc.RootElement.TryGetProperty("result", out var arr))
            {
                foreach (var item in arr.EnumerateArray())
                {
                    string cnName = item.GetProperty("name").GetString() ?? "";
                    double calories = 0;
                    if (item.TryGetProperty("calorie", out var calElem))
                        double.TryParse(calElem.GetString(), out calories);

                    string krName = await TranslateToKorean(cnName);
                    resultList.Add(new FoodItem
                    {
                        NameCn = cnName,
                        NameKr = krName,
                        Calories = calories
                    });
                }
            }

            return resultList;
        }

        // ===================== (3) 获取百度 access_token =====================
        private async Task<string> GetBaiduAccessTokenAsync()
        {
            string ak = "sjnmbxCQVqYeIV5PIVIJ9IV7";   // 你的API Key
            string sk = "MdTvV021X4GbaENGr2mMsYHItxOfJ93A"; // 你的Secret Key

            var url =
                $"https://aip.baidubce.com/oauth/2.0/token" +
                $"?grant_type=client_credentials&client_id={ak}&client_secret={sk}";

            var resp = await _httpClient.GetAsync(url);
            string txt = await resp.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(txt);
            var root = doc.RootElement;

            if (root.TryGetProperty("access_token", out var token))
                return token.GetString()!;

            var err = root.TryGetProperty("error_description", out var desc)
                ? desc.GetString() : txt;
            throw new InvalidOperationException($"Token 获取失败: {err}");
        }

        // ===================== (4) 调用菜品识别 API =====================
        private async Task<string> CallDishApiAsync(string accessToken, string imagePath)
        {
            byte[] imageBytes = await System.IO.File.ReadAllBytesAsync(imagePath);
            string base64 = Convert.ToBase64String(imageBytes);
            string encoded = WebUtility.UrlEncode(base64);
            string body = $"image={encoded}&top_num=5";

            string url = $"https://aip.baidubce.com/rest/2.0/image-classify/v2/dish?access_token={accessToken}";
            var content = new StringContent(body, Encoding.UTF8, "application/x-www-form-urlencoded");

            var response = await _httpClient.PostAsync(url, content);
            return await response.Content.ReadAsStringAsync();
        }

        // ===================== (5) 翻译接口（中 → 韩） =====================
        private async Task<string> TranslateToKorean(string text)
        {
            string ak = "sjnmbxCQVqYeIV5PIVIJ9IV7";
            string sk = "MdTvV021X4GbaENGr2mMsYHItxOfJ93A";

            string token = await GetBaiduAccessTokenAsync();
            string transUrl = $"https://aip.baidubce.com/rpc/2.0/mt/texttrans/v1?access_token={token}";

            var body = new
            {
                from = "zh",
                to = "kor",
                q = text
            };

            var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(transUrl, content);
            string json = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("result", out var result) &&
                result.TryGetProperty("trans_result", out var arr))
            {
                return arr[0].GetProperty("dst").GetString() ?? text;
            }

            return text;
        }

        // ===================== (6) DTO =====================
        private class FoodItem
        {
            public string NameCn { get; set; } = "";
            public string NameKr { get; set; } = "";
            public double Calories { get; set; }
        }
    }
}
