CREATE TABLE Users (
	id INT NOT NULL,
	username varchar(1000) NOT NULL,
	PRIMARY KEY (id)
);

CREATE TABLE ToDoTask (
	Id  INT NOT IDENTITY(1,1) NULL,
	UserId INT NOT NULL,
	Name varchar(100) NOT NULL,
	IsCompleted BIT NOT NULL,
	PRIMARY KEY (Id )
);

ALTER TABLE ToDoTask ADD CONSTRAINT ToDoTask_fk0 FOREIGN KEY (UserId) REFERENCES Users(id);
