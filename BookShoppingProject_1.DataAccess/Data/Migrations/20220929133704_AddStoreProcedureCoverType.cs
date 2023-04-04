using Microsoft.EntityFrameworkCore.Migrations;

namespace BookShoppingProject_1.DataAccess.Migrations
{
    public partial class AddStoreProcedureCoverType : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            //Display
            migrationBuilder.Sql(@"CREATE PROCEDURE GetCoverTypes
            AS
	        SELECT*from CoverTypes");

            //Find
            migrationBuilder.Sql(@"CREATE PROCEDURE GetCoverType
            @Id int
            AS
	        SELECT*from CoverTypes where Id=@Id");

            //Create
            migrationBuilder.Sql(@"CREATE PROCEDURE CreateCoverType
            @Name varchar(50)
            AS
            insert CoverTypes values(@Name)");

            //Update
            migrationBuilder.Sql(@"CREATE PROCEDURE UpdateCoverType
            @Id int,
            @Name varchar(50)
            AS
	        update CoverTypes set Name=@Name where Id=@Id");

            //Delete
            migrationBuilder.Sql(@"CREATE PROCEDURE DeleteCoverType
            @Id int
            AS
	        Delete CoverTypes where Id=@Id");

        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
