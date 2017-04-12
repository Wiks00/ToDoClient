using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Entity;
using System.Threading;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using WebApi.Concrete;
using WebApi.DAL;
using WebApi.Helpers;
using WebApi.Models;

namespace WebApi.Services
{
    public class SyncService : IDisposable
    {
        private readonly ToDoService todoService;
        private readonly TempDBEntities dbEntities;
        private readonly BlockingCollection<CommunicationMessage> requestsList; 

        public CancellationTokenSource CancellationToken { get; set; }

        public SyncService()
        {
            todoService = new ToDoService();
            dbEntities = new TempDBEntities();
            requestsList = new BlockingCollection<CommunicationMessage>();
        }

        public async Task<IList<ToDoItemModel>> GetToDoItemsAsync(int userId)
        {
            var toDos = dbEntities.ToDoTask.Where(item => item.UserId == userId).ToList();

            List<ToDoItemModel> response = await Task.Run(() => todoService.GetItems(userId).ToList());

            foreach (var item in response)
            {
                var toDoItem = toDos.FirstOrDefault(toDo => toDo.Name.Trim().Equals(item.Name.Trim(), StringComparison.InvariantCultureIgnoreCase));

                if (!ReferenceEquals(toDoItem, null))
                {
                    toDoItem.Name = item.Name.GetName(true) + item.ToDoId;

                    dbEntities.Entry(toDoItem).State = EntityState.Modified;
                }

                if (!toDos.Any(toDo => toDo.Name.GetName().Equals(item.Name.GetName())))
                {
                    dbEntities.ToDoTask.Add(item.ToOrmEntity());
                }

            }

            dbEntities.SaveChanges();  

            return dbEntities.ToDoTask.Where(item => item.UserId == userId).ToList().Select(item => item.ToUIEntity()).ToList();
        }

        public void AddToDoItem(ToDoItemModel toDoItem)
        {
            toDoItem.Name += "," + Guid.NewGuid();

            dbEntities.ToDoTask.Add(toDoItem.ToOrmEntity());
            dbEntities.SaveChanges();

            AddToRequestsList(toDoItem, Operation.Add);
        }

        public void UpdateToDoItem(ToDoItemModel toDoItem)
        {
            var toDoTask = dbEntities.ToDoTask.Find(toDoItem.ToDoId);

            toDoTask.IsCompleted = toDoItem.IsCompleted;

            dbEntities.Entry(toDoTask).State = EntityState.Modified;
            dbEntities.SaveChanges();

            toDoItem.ToDoId = toDoItem.GetId();

            AddToRequestsList(toDoItem, Operation.Update);
        }

        public void DeleteToDoItem(int id)
        {

            var toDoTask = dbEntities.ToDoTask.Find(id);

            dbEntities.ToDoTask.Remove(toDoTask);

            dbEntities.SaveChanges();
        }

        public void ForceSync(CancellationTokenSource cancellationTokenSource)
        {
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
        private void AddToRequestsList(ToDoItemModel toDoItem, Operation operation)
        {
            requestsList.Add(new CommunicationMessage { Operation = operation, ToDoItem = toDoItem });

            ForceSync(CancellationToken ?? new CancellationTokenSource());
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