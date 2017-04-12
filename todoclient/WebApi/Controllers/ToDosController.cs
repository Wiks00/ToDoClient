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

        public async Task<HttpResponseMessage> Put(ToDoItemModel todo)
        {
            return await GetSyncService.UpdateToDoItemAsync(todo);
        }

        public async Task<HttpResponseMessage> Delete(int id)
        {
            return await GetSyncService.DeleteToDoItemAsync(id);
        }

        public async Task<HttpResponseMessage> Post(ToDoItemModel todo)
        {
            return await GetSyncService.AddToDoItemAsync(todo);
        }       

    }
}
