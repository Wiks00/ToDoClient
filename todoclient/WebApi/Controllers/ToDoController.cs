using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using WebApi.Concrete;
using WebApi.Models;
using WebApi.Services;

namespace WebApi.Controllers
{
    public class ToDoController : ApiController
    {
        private readonly ToDoService todoService = new ToDoService();
        private readonly UserService userService = new UserService();

        private SyncMaster GetSyncMaster { get; } = new SyncMaster();

        // GET api/todo
        public async Task<IList<ToDoItemModel>> Get()
        {
            var userId = userService.GetOrCreateUser();
            var toDoItems =  await Task.Run(() => todoService.GetItems(userId));



            ////ToDo: Add DB and sync Logic 

            return toDoItems;
        }

        // GET api/todo/5
        public string Get(int id)
        {
            return "value";
        }

        // POST api/todo
        public void Post([FromBody]string value)
        {
        }

        // PUT api/todo/5
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/todo/5
        public void Delete(int id)
        {
        }
    }
}
