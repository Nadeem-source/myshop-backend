using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using RegisterApi.Dtos;
using RegisterApi.Models;
using System.Data;

[ApiController]
[Route("api")]
public class ProductsController : ControllerBase
{
    private readonly IConfiguration _config;

    public ProductsController(IConfiguration config)
    {
        _config = config;
    }

    // =========================
    // GET ALL PRODUCTS
    // =========================
    [HttpGet("products")]
    public async Task<IActionResult> GetProducts()
    {
        var productDict = new Dictionary<int, Product>();

        using SqlConnection con = new SqlConnection(
            _config.GetConnectionString("DefaultConnection"));

        using SqlCommand cmd = new SqlCommand("sp_GetAllProducts", con);
        cmd.CommandType = CommandType.StoredProcedure;

        await con.OpenAsync();
        using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            int productId = reader.GetInt32(0);

            if (!productDict.ContainsKey(productId))
            {
                productDict[productId] = new Product
                {
                    Id = productId,
                    Title = reader.GetString(1),
                    Description = reader.GetString(2),
                    Category = reader.GetString(3),
                    Brand = reader.GetString(4),
                    Price = reader.GetDecimal(5),
                    DiscountPercentage = Convert.ToDouble(reader.GetValue(6)),
                    Rating = Convert.ToDouble(reader.GetValue(7)),
                    Stock = reader.GetInt32(8),
                    Thumbnail = reader.IsDBNull(9) ? "": reader.GetValue(9).ToString(),
                    Sku = reader.GetString(10),
                    AvailabilityStatus = reader.GetString(11),
                    CreatedAt = reader.GetDateTime(12),
                    SellerId = reader.GetInt32(13),
                    Images = new List<string>()
                };
            }

            if (!reader.IsDBNull(13))
            {
                string img = reader.IsDBNull(13) ? "" : reader.GetValue(13).ToString();

                if (!productDict[productId].Images.Contains(img))
                    productDict[productId].Images.Add(img);
            }
        }

        return Ok(productDict.Values);
    }

    // =========================
    // GET PRODUCT BY ID
    // =========================
    [HttpGet("product/{id}")]
    public IActionResult GetProductById(int id)
    {
        string conn = _config.GetConnectionString("DefaultConnection");
        Product product = null;

        using SqlConnection con = new SqlConnection(conn);
        using SqlCommand cmd = new SqlCommand("sp_GetProductById", con);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("@Id", id);

        con.Open();
        using var reader = cmd.ExecuteReader();

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
                Thumbnail = reader["Thumbnail"].ToString(),
                Sku = reader["Sku"].ToString(),
                AvailabilityStatus = reader["AvailabilityStatus"].ToString(),
                CreatedAt = Convert.ToDateTime(reader["CreatedAt"]),
                SellerId = Convert.ToInt32(reader["SellerId"]) // ✅ ADD THIS

            };
        }

        if (product == null)
            return NotFound();

        return Ok(product);
    }

    // =========================
    // UPDATE PRODUCT (URL + UPLOAD + MULTI)
    // =========================
    [HttpPut("products/{id}")]
    public IActionResult UpdateProduct(
        int id,
        [FromForm] UpdateProductDto dto,
        [FromForm] List<IFormFile>? imageFiles
    )
    {
        string conn = _config.GetConnectionString("DefaultConnection");

        using SqlConnection con = new SqlConnection(conn);
        con.Open();

        // 🔹 UPDATE MAIN PRODUCT
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
            cmd.Parameters.AddWithValue("@Thumbnail", dto.Thumbnail);
            cmd.Parameters.AddWithValue("@Sku", dto.Sku);
            cmd.Parameters.AddWithValue("@AvailabilityStatus", dto.AvailabilityStatus);

            cmd.ExecuteNonQuery();
        }

        // 🌐 INSERT INTERNET IMAGE URLS
        if (dto.ImageUrls != null)
        {
            foreach (var url in dto.ImageUrls)
            {
                using SqlCommand imgCmd = new SqlCommand(
                    "INSERT INTO ProductImages (ProductId, ImageUrl) VALUES (@Pid,@Url)",
                    con
                );
                imgCmd.Parameters.AddWithValue("@Pid", id);
                imgCmd.Parameters.AddWithValue("@Url", url);
                imgCmd.ExecuteNonQuery();
            }
        }

        // 📁 INSERT UPLOADED IMAGES
        if (imageFiles != null)
        {
            foreach (var file in imageFiles)
            {
                var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
                var path = Path.Combine("wwwroot/uploads", fileName);

                using var stream = new FileStream(path, FileMode.Create);
                file.CopyTo(stream);

                using SqlCommand imgCmd = new SqlCommand(
                    "INSERT INTO ProductImages (ProductId, ImageUrl) VALUES (@Pid,@Url)",
                    con
                );
                imgCmd.Parameters.AddWithValue("@Pid", id);
                imgCmd.Parameters.AddWithValue("@Url", "/uploads/" + fileName);
                imgCmd.ExecuteNonQuery();
            }
        }

        return Ok(new { message = "Product updated successfully" });
    }

    // =========================
    // DELETE PRODUCT (WITH IMAGES)
    // =========================
    [HttpDelete("products/{id}")]
    public IActionResult DeleteProduct(int id)
    {
        string conn = _config.GetConnectionString("DefaultConnection");

        using SqlConnection con = new SqlConnection(conn);
        con.Open();

        using SqlCommand cmd = new SqlCommand("sp_DeleteProduct", con);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("@Id", id);
        cmd.ExecuteNonQuery();

        return Ok(new { message = "Product deleted successfully" });
    }
    [HttpGet("products/seller/{sellerId}")]
    public IActionResult GetProductsBySeller(int sellerId)
    {
        string conn = _config.GetConnectionString("DefaultConnection");

        List<Product> products = new List<Product>();

        using SqlConnection con = new SqlConnection(conn);
        using SqlCommand cmd = new SqlCommand(
            "SELECT * FROM Products WHERE SellerId = @SellerId",
            con);

        cmd.Parameters.AddWithValue("@SellerId", sellerId);

        con.Open();
        using SqlDataReader reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            products.Add(new Product
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
                Thumbnail = reader["Thumbnail"].ToString(),
                Sku = reader["Sku"].ToString(),
                AvailabilityStatus = reader["AvailabilityStatus"].ToString(),
                CreatedAt = Convert.ToDateTime(reader["CreatedAt"]),
                SellerId = Convert.ToInt32(reader["SellerId"])
            });
        }

        return Ok(products);
    }
}
