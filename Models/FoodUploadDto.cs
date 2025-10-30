using Microsoft.AspNetCore.Http;

namespace HealthyApi.Models
{
    public class FoodUploadDto
    {
        public IFormFile? Image { get; set; }    // 上传的图片
        public int UserId { get; set; }          // 用户ID
        public string MealType { get; set; } = ""; // 早餐/午餐/晚餐/加餐
    }
}
