using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace RegisterApi.Controllers
{
    [ApiController]
    [Route("api/search")]
    public class SearchController : ControllerBase
    {
        private readonly IConfiguration _config;

        public SearchController(IConfiguration config)
        {
            _config = config;
        }

        [HttpGet]
        public IActionResult Search([FromQuery] string q)
        {
            if (string.IsNullOrWhiteSpace(q))
                return Ok(new List<object>());

            string conn = _config.GetConnectionString("DefaultConnection");
            List<object> results = new();

            using (SqlConnection con = new SqlConnection(conn))
            using (SqlCommand cmd = new SqlCommand("sp_SearchProducts", con))
            {
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Query", q);

                con.Open();
                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    results.Add(new
                    {
                        id = reader["Id"],
                        title = reader["Title"]?.ToString(),
                        description = reader["Description"] == DBNull.Value
                            ? ""
                            : reader["Description"].ToString(),

                        price = reader["Price"] == DBNull.Value
                            ? 0
                            : Convert.ToDecimal(reader["Price"]),

                        category = reader["Category"] == DBNull.Value
                            ? ""
                            : reader["Category"].ToString(),

                        brand = reader["Brand"] == DBNull.Value
                            ? ""
                            : reader["Brand"].ToString(),

                        image = reader["Thumbnail"] == DBNull.Value
                            ? ""
                            : reader["Thumbnail"].ToString(),

                        stock = reader["Stock"] == DBNull.Value
                            ? 0
                            : Convert.ToInt32(reader["Stock"])
                    });
                }
            }

            return Ok(results);
        }
    }
}
