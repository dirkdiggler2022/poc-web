using Microsoft.AspNetCore.Mvc;
using Private.Web.Models;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Private.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StonksController : ControllerBase
    {
        // GET: api/<StonksController>
        [HttpGet]
        public IEnumerable<StonkQuote> Get()
        {
            var rand = new Random();

            return StonkQuote.GetQuotes(rand.Next(100, 1000));
        }

        // GET api/<StonksController>/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            return "value";
        }

        // POST api/<StonksController>
        [HttpPost]
        public void Post([FromBody] string value)
        {
        }

        // PUT api/<StonksController>/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/<StonksController>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }


    }
}
