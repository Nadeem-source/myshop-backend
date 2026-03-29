using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using RegisterApi.Dtos;
using RegisterApi.Models;
using System.Data;


namespace RegisterApi.Controllers
{
    [ApiController]
    [Route("api/admin")]
    public class AdminProductController : ControllerBase
    {
        private readonly IConfiguration _config;

        public AdminProductController(IConfiguration config)
        {
            _config = config;
        }

        /* ================= IMAGE SAVE HELPER ================= */
        private static async Task<string> SaveImageAsync(IFormFile file)
        {
            var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");

            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
            var fullPath = Path.Combine(folderPath, fileName);

            using var stream = new FileStream(fullPath, FileMode.Create);
            await file.CopyToAsync(stream);

            return "/uploads/" + fileName;
        }

        /* ================= ADD PRODUCT ================= */
        [HttpPost("add-product")]
        public async Task<IActionResult> AddProduct([FromForm] AddProductDto dto)
        {
            try
            {
                /* ========== VALIDATION ========== */
                if (string.IsNullOrWhiteSpace(dto.Title))
                    return BadRequest("Title is required");

                if (dto.Price <= 0)
                    return BadRequest("Price must be greater than 0");
                if (dto.Stock < 0)
                {
                    return BadRequest("Stock must be 0 or greater");
                }


                bool hasUrlImages = dto.ImageUrls != null && dto.ImageUrls.Any(x => !string.IsNullOrWhiteSpace(x));
                bool hasFileImages = dto.ImageFiles != null && dto.ImageFiles.Count > 0;

                if (!hasUrlImages && !hasFileImages)
                    return BadRequest("At least one image is required");

                /* ========== THUMBNAIL LOGIC ========== */
                string thumbnail = dto.Thumbnail ?? "";

                // 🌐 INTERNET IMAGE SELECTED
                if (!string.IsNullOrWhiteSpace(thumbnail) && thumbnail.StartsWith("http"))
                {
                    // use as-is
                }
                // 📁 UPLOAD IMAGE SELECTED
                else if (hasFileImages)
                {
                    thumbnail = await SaveImageAsync(dto.ImageFiles![0]);
                }
                // 🌐 FALLBACK FIRST URL
                else if (hasUrlImages)
                {
                    thumbnail = dto.ImageUrls!.First(x => !string.IsNullOrWhiteSpace(x));
                }


                int productId;
                string connStr = _config.GetConnectionString("DefaultConnection");

                /* ========== INSERT PRODUCT ========== */
                using (SqlConnection con = new SqlConnection(connStr))
                using (SqlCommand cmd = new SqlCommand("sp_AddProduct", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@Title", dto.Title);
                    cmd.Parameters.AddWithValue("@Description", dto.Description);
                    cmd.Parameters.AddWithValue("@Category", dto.Category);
                    cmd.Parameters.AddWithValue("@Brand", dto.Brand);
                    cmd.Parameters.AddWithValue("@Price", dto.Price);
                    cmd.Parameters.AddWithValue("@DiscountPercentage", dto.DiscountPercentage);
                    cmd.Parameters.AddWithValue("@Rating", dto.Rating);
                    cmd.Parameters.AddWithValue("@Stock", dto.Stock);
                    cmd.Parameters.AddWithValue("@Thumbnail", thumbnail);
                    cmd.Parameters.AddWithValue("@ImageUrl", thumbnail);

                    cmd.Parameters.AddWithValue("@Sku", dto.Sku);
                    cmd.Parameters.AddWithValue("@AvailabilityStatus", dto.AvailabilityStatus);
                    cmd.Parameters.AddWithValue("@SellerId", dto.SellerId); 

                    con.Open();
                    productId = Convert.ToInt32(cmd.ExecuteScalar());
                }

                /* ========== INSERT URL IMAGES ========== */
                if (hasUrlImages)
                {
                    foreach (var url in dto.ImageUrls!)
                    {
                        if (string.IsNullOrWhiteSpace(url)) continue;

                        using SqlConnection con = new SqlConnection(connStr);
                        using SqlCommand cmd = new SqlCommand("sp_AddProductImage", con);

                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@ProductId", productId);
                        cmd.Parameters.AddWithValue("@ImageUrl", url);

                        con.Open();
                        cmd.ExecuteNonQuery();
                    }
                }

                /* ========== INSERT FILE IMAGES ========== */
                if (hasFileImages)
                {
                    foreach (var file in dto.ImageFiles!)
                    {
                        var imageUrl = await SaveImageAsync(file);

                        using SqlConnection con = new SqlConnection(connStr);
                        using SqlCommand cmd = new SqlCommand("sp_AddProductImage", con);

                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@ProductId", productId);
                        cmd.Parameters.AddWithValue("@ImageUrl", imageUrl);

                        con.Open();
                        cmd.ExecuteNonQuery();
                    }
                }

                return Ok(new
                {
                    message = "Product added successfully",
                    productId
                });
            }
            catch (SqlException ex)
            {
                return StatusCode(500, "SQL Error: " + ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Server Error: " + ex.Message);
            }
        }



        [HttpDelete("delete-product/{id}")]
        public IActionResult DeleteProduct(int id)
        {
            using SqlConnection con = new SqlConnection(
                _config.GetConnectionString("DefaultConnection"));

            using SqlCommand cmd = new SqlCommand(
                "DELETE FROM Products WHERE Id=@Id", con);

            cmd.Parameters.AddWithValue("@Id", id);
            con.Open();

            int rows = cmd.ExecuteNonQuery();
            if (rows == 0) return NotFound();

            return Ok();
        }

        [HttpPut("update-product/{id}")]
        public async Task<IActionResult> UpdateProduct(
     int id,
     [FromForm] UpdateProductDto dto)
        {
            string conn = _config.GetConnectionString("DefaultConnection");

            /* ===== DELETE OLD IMAGES ===== */
            using (SqlConnection con = new SqlConnection(conn))
            using (SqlCommand cmd = new SqlCommand(
                "DELETE FROM ProductImages WHERE ProductId=@Id", con))
            {
                cmd.Parameters.AddWithValue("@Id", id);
                con.Open();
                cmd.ExecuteNonQuery();
            }

            string primaryImage = dto.Thumbnail ?? "";

            using (SqlConnection con = new SqlConnection(conn))
            using (SqlCommand cmd = new SqlCommand("sp_UpdateProduct", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@Id", id);
                cmd.Parameters.AddWithValue("@Title", dto.Title);
                cmd.Parameters.AddWithValue("@Description", dto.Description);
                cmd.Parameters.AddWithValue("@Category", dto.Category);
                cmd.Parameters.AddWithValue("@Brand", dto.Brand);
                cmd.Parameters.AddWithValue("@Price", dto.Price);
                cmd.Parameters.AddWithValue("@DiscountPercentage", dto.DiscountPercentage);
                cmd.Parameters.AddWithValue("@Rating", dto.Rating);
                cmd.Parameters.AddWithValue("@Stock", dto.Stock);
                cmd.Parameters.AddWithValue("@Sku", dto.Sku);
                cmd.Parameters.AddWithValue("@AvailabilityStatus", dto.AvailabilityStatus);
                cmd.Parameters.AddWithValue("@Thumbnail", primaryImage);
                cmd.Parameters.AddWithValue("@ImageUrl", primaryImage);

                con.Open();
                cmd.ExecuteNonQuery();
            }

            if (dto.ImageUrls != null)
            {
                foreach (var url in dto.ImageUrls)
                {
                    if (string.IsNullOrWhiteSpace(url)) continue;

                    using SqlConnection con = new SqlConnection(conn);
                    using SqlCommand cmd = new SqlCommand("sp_AddProductImage", con);

                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@ProductId", id);
                    cmd.Parameters.AddWithValue("@ImageUrl", url);

                    con.Open();
                    cmd.ExecuteNonQuery();
                }
            }

            if (dto.ImageFiles != null)
            {
                foreach (var file in dto.ImageFiles)
                {
                    var imageUrl = await SaveImageAsync(file);

                    using SqlConnection con = new SqlConnection(conn);
                    using SqlCommand cmd = new SqlCommand("sp_AddProductImage", con);

                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@ProductId", id);
                    cmd.Parameters.AddWithValue("@ImageUrl", imageUrl);

                    con.Open();
                    cmd.ExecuteNonQuery();
                }
            }
       
            return Ok(new { message = "Updated successfully" });
        }
        [HttpGet("/api/editproduct/{id}")]
        public IActionResult GetProduct(int id)
        {
            string conn = _config.GetConnectionString("DefaultConnection");

            Product product = null;
            List<string> images = new List<string>();

            using (SqlConnection con = new SqlConnection(conn))
            {
                con.Open();

                // ===== GET PRODUCT =====
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT * FROM Products WHERE Id=@Id", con))
                {
                    cmd.Parameters.AddWithValue("@Id", id);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            product = new Product
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                Title = reader["Title"].ToString(),
                                Description = reader["Description"].ToString(),
                                Category = reader["Category"].ToString(),
                                Brand = reader["Brand"].ToString(),
                                Price = Convert.ToDecimal(reader["Price"]),
                                DiscountPercentage = Convert.ToDouble(reader["DiscountPercentage"]),
                                Rating = Convert.ToDouble(reader["Rating"]),
                                Stock = Convert.ToInt32(reader["Stock"]),
                                Sku = reader["Sku"].ToString(),
                                AvailabilityStatus = reader["AvailabilityStatus"].ToString(),
                                Thumbnail = reader["Thumbnail"].ToString()
                            };
                        }
                    }
                }

                if (product == null)
                    return NotFound();

                // ===== GET IMAGES =====
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT ImageUrl FROM ProductImages WHERE ProductId=@Id", con))
                {
                    cmd.Parameters.AddWithValue("@Id", id);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            images.Add(reader["ImageUrl"].ToString());
                        }
                    }
                }
            }

            return Ok(new
            {
                id = product.Id,
                title = product.Title,
                description = product.Description,
                category = product.Category,
                brand = product.Brand,
                price = product.Price,
                discountPercentage = product.DiscountPercentage,
                rating = product.Rating,
                stock = product.Stock,
                sku = product.Sku,
                availabilityStatus = product.AvailabilityStatus,
                thumbnail = product.Thumbnail,
                images = images   // 🔥 IMPORTANT
            });
        }
    }

}