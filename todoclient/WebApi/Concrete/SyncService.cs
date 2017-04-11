using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Entity;
using System.Threading;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;
using WebApi.DAL;
using WebApi.Helpers;
using WebApi.Models;
using WebApi.Services;

namespace WebApi.Concrete
{
    public class SyncService : IDisposable
    {
        private readonly int maxRequestCountBeforeSync;

        private readonly ToDoService todoService;
        private readonly TempDBEntities dbEntities;


        private static readonly object syncObject;

        private BlockingCollection<CommunicationMessage> requestsList; 
        private bool syncToken;

        public CancellationTokenSource CancellationToken { get; set; }

        public SyncService(int maxRequestCountBeforeSync = 10)
        {
            this.maxRequestCountBeforeSync = maxRequestCountBeforeSync;

            syncToken = false;

            var cookie = HttpContext.Current.Request.Cookies["syncToken"];

            if (ReferenceEquals(cookie,null) || !bool.TryParse(cookie.Value, out syncToken))
            {
                cookie = new HttpCookie("syncToken", syncToken.ToString())
                {
                    Expires = DateTime.Today.AddMonths(1)
                };

                HttpContext.Current.Response.SetCookie(cookie);
            }

            todoService = new ToDoService();
            dbEntities = new TempDBEntities();
            requestsList = new BlockingCollection<CommunicationMessage>(maxRequestCountBeforeSync);
        }

        public async Task<IList<ToDoItemModel>> GetToDoItems(int userId)
        {
            List<ToDoItemModel> response = null;

            if (IsSyncNeeded())
            {
                response = await Task.Run(() => todoService.GetItems(userId).ToList());

                dbEntities.ToDoTask.AddRange(response.Select(item => item.ToOrmEntity()).ToList());
                dbEntities.SaveChanges();
            }
            else
            {
                response = dbEntities.ToDoTask.Where(item => item.UserId == userId).Select(item => item.ToUIEntity()).ToList();
            }
          
            //ToDo: Add exeption handling

            syncToken = true;

            return response;
        }

        public int AddToDoItem(ToDoItemModel toDoItem)
        {
            toDoItem.Name += Guid.NewGuid();
            
            dbEntities.ToDoTask.Add(toDoItem.ToOrmEntity());
            dbEntities.SaveChanges();
            //ToDo: Add exeption handling

            AddToRequestsList(toDoItem, Operation.Add);

            return toDoItem.ToDoId;
        }

        public void UpdateToDoItem(ToDoItemModel toDoItem)
        {
            CommunicationMessage request;  
                 
            request = requestsList.FirstOrDefault(
                        msg => msg.Operation.Equals(Operation.Add) && msg.ToDoItem.Equals(toDoItem));

            if (!ReferenceEquals(request, null))
            {
                lock (syncObject)
                {
                    request.ToDoItem = toDoItem;
                }
                
                return;
            }

            var toDoTask = dbEntities.ToDoTask.Find(toDoItem.GetId());

            toDoTask.Name = toDoItem.Name;
            toDoTask.IsCompleted = toDoItem.IsCompleted;
            toDoTask.UserId = toDoItem.UserId;

            dbEntities.Entry(toDoTask).State = EntityState.Modified;
            dbEntities.SaveChanges();

            //ToDo: Add exeption handling

            AddToRequestsList(toDoItem, Operation.Update);
        }

        public void DeleteToDoItem(ToDoItemModel toDoItem)
        {        
            CommunicationMessage request;

            request = requestsList.FirstOrDefault(
                        msg => msg.Operation.Equals(Operation.Add) || msg.Operation.Equals(Operation.Update) && msg.ToDoItem.Equals(toDoItem));

            if (!ReferenceEquals(request, null))
            {
                //ToDo: unreal to delete specific item
            }

            var toDoTask = dbEntities.ToDoTask.Find(toDoItem.GetId());

            dbEntities.ToDoTask.Remove(toDoTask);

            //ToDo: Add exeption handling

            AddToRequestsList(toDoItem, Operation.Delete);
        }

        public void ForceSync(CancellationTokenSource cancellationTokenSource)
        {
            requestsList.CompleteAdding();

            foreach (var request in requestsList.GetConsumingEnumerable())
            {
                Task.Run(() =>
                {
                    if (cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        //ToDo: Add cancellation handling
                        return;
                    }

                    SolveMethod(request);
                });
            }
            
        }

        private bool IsSyncNeeded()
        {
            return !syncToken || maxRequestCountBeforeSync <= requestsList.Count;
        }

        private void AddToRequestsList(ToDoItemModel toDoItem, Operation operation)
        {
            if (IsSyncNeeded())
                    ForceSync(CancellationToken ?? new CancellationTokenSource());

            requestsList.Add(new CommunicationMessage { Operation = operation, ToDoItem = toDoItem });
        }


        private void SolveMethod(CommunicationMessage message)
        {
            switch (message.Operation)
            {
                case Operation.Add:
                    todoService.CreateItem(message.ToDoItem);
                    break;

                case Operation.Update:
                    todoService.UpdateItem(message.ToDoItem);
                    break;

                case Operation.Delete:
                    todoService.DeleteItem(message.ToDoItem.GetId());
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void Dispose()
        {
            requestsList.Dispose();
        }
    }
}