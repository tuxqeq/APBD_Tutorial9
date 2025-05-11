using Tutorial9.DTOs;
using Tutorial9.Services;
using Microsoft.AspNetCore.Mvc;


namespace Tutorial9.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WarehouseController : ControllerBase
    {
        private readonly IWarehouseService _service;
        public WarehouseController(IWarehouseService service)
        {
            _service = service;
        }

        [HttpPost("add-inline")]
        public async Task<IActionResult> AddProductInline([FromBody] AddProductRequestDto dto)
        {
            try
            {
                int newId = await _service.AddProductToWarehouseAsync(dto);
                return Ok(new AddProductResponseDto { NewId = newId });
            }
            catch (KeyNotFoundException knf)
            {
                return NotFound(new { Error = knf.Message });
            }
            catch (ArgumentException argEx)
            {
                return BadRequest(new { Error = argEx.Message });
            }
            catch (InvalidOperationException opEx)
            {
                return BadRequest(new { Error = opEx.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = "Internal server error." });
            }
        }

        [HttpPost("add-proc")]
        public async Task<IActionResult> AddProductWithProc([FromBody] AddProductRequestDto dto)
        {
            try
            {
                int newId = await _service.AddProductToWarehouseUsingProcAsync(dto);
                return Ok(new AddProductResponseDto { NewId = newId });
            }
            catch (KeyNotFoundException knf)
            {
                return NotFound(new { Error = knf.Message });
            }
            catch (ArgumentException argEx)
            {
                return BadRequest(new { Error = argEx.Message });
            }
            catch (InvalidOperationException opEx)
            {
                return BadRequest(new { Error = opEx.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = "Internal server error." });
            }
        }
    }
}