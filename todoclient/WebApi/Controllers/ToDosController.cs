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
    public class ToDosController : ApiController
    {
        private SyncService GetSyncService { get; } = new SyncService();

        public async Task<IList<ToDoItemModel>> Get(int userId)
        {
            return await GetSyncService.GetToDoItemsAsync(userId);
        }

        public void Put(ToDoItemModel todo)
        {
            GetSyncService.UpdateToDoItem(todo);
        }

        public void Delete(int id)
        {
            GetSyncService.DeleteToDoItem(id);
        }

        public void Post(ToDoItemModel todo)
        {
            GetSyncService.AddToDoItem(todo);
        }       

    }
}
