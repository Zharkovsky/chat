namespace AngelsChat.Server.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Init2 : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Messages",
                c => new
                    {
                        MessageId = c.Int(nullable: false, identity: true),
                        Text = c.String(),
                        Date = c.DateTime(nullable: false),
                        FileName = c.String(),
                        FileWeight = c.Long(),
                        Discriminator = c.String(nullable: false, maxLength: 128),
                        User_Name = c.String(maxLength: 128),
                        User_Name1 = c.String(nullable: false, maxLength: 128),
                        Room_Id = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.MessageId)
                .ForeignKey("dbo.Users", t => t.User_Name)
                .ForeignKey("dbo.Users", t => t.User_Name1, cascadeDelete: true)
                .ForeignKey("dbo.Rooms", t => t.Room_Id, cascadeDelete: true)
                .Index(t => t.User_Name)
                .Index(t => t.User_Name1)
                .Index(t => t.Room_Id);
            
            CreateTable(
                "dbo.BinaryContents",
                c => new
                    {
                        Id = c.Int(nullable: false),
                        Content = c.Binary(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Messages", t => t.Id)
                .Index(t => t.Id);
            
            CreateTable(
                "dbo.Rooms",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(),
                        OwnerName = c.String(maxLength: 128),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Users", t => t.OwnerName)
                .Index(t => t.OwnerName);
            
            CreateTable(
                "dbo.Users",
                c => new
                    {
                        Name = c.String(nullable: false, maxLength: 128),
                        Password = c.String(),
                        Date = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Name);
            
            CreateTable(
                "dbo.Images",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Picture = c.Binary(),
                        Width = c.Int(nullable: false),
                        Height = c.Int(nullable: false),
                        User_Name = c.String(nullable: false, maxLength: 128),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Users", t => t.User_Name)
                .Index(t => t.User_Name);
            
            CreateTable(
                "dbo.Videos",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Date = c.DateTime(nullable: false),
                        User_Name = c.String(nullable: false, maxLength: 128),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Users", t => t.User_Name, cascadeDelete: true)
                .Index(t => t.User_Name);
            
            CreateTable(
                "dbo.VideoElements",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Data = c.Binary(),
                        InternalData = c.String(),
                        VideoId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Videos", t => t.VideoId, cascadeDelete: true)
                .Index(t => t.VideoId);
            
            CreateTable(
                "dbo.UserRooms",
                c => new
                    {
                        RoomId = c.Int(nullable: false),
                        UserId = c.String(nullable: false, maxLength: 128),
                    })
                .PrimaryKey(t => new { t.RoomId, t.UserId })
                .ForeignKey("dbo.Rooms", t => t.RoomId, cascadeDelete: true)
                .ForeignKey("dbo.Users", t => t.UserId, cascadeDelete: true)
                .Index(t => t.RoomId)
                .Index(t => t.UserId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.UserRooms", "UserId", "dbo.Users");
            DropForeignKey("dbo.UserRooms", "RoomId", "dbo.Rooms");
            DropForeignKey("dbo.Rooms", "OwnerName", "dbo.Users");
            DropForeignKey("dbo.Messages", "Room_Id", "dbo.Rooms");
            DropForeignKey("dbo.Videos", "User_Name", "dbo.Users");
            DropForeignKey("dbo.VideoElements", "VideoId", "dbo.Videos");
            DropForeignKey("dbo.Messages", "User_Name1", "dbo.Users");
            DropForeignKey("dbo.Images", "User_Name", "dbo.Users");
            DropForeignKey("dbo.Messages", "User_Name", "dbo.Users");
            DropForeignKey("dbo.BinaryContents", "Id", "dbo.Messages");
            DropIndex("dbo.UserRooms", new[] { "UserId" });
            DropIndex("dbo.UserRooms", new[] { "RoomId" });
            DropIndex("dbo.VideoElements", new[] { "VideoId" });
            DropIndex("dbo.Videos", new[] { "User_Name" });
            DropIndex("dbo.Images", new[] { "User_Name" });
            DropIndex("dbo.Rooms", new[] { "OwnerName" });
            DropIndex("dbo.BinaryContents", new[] { "Id" });
            DropIndex("dbo.Messages", new[] { "Room_Id" });
            DropIndex("dbo.Messages", new[] { "User_Name1" });
            DropIndex("dbo.Messages", new[] { "User_Name" });
            DropTable("dbo.UserRooms");
            DropTable("dbo.VideoElements");
            DropTable("dbo.Videos");
            DropTable("dbo.Images");
            DropTable("dbo.Users");
            DropTable("dbo.Rooms");
            DropTable("dbo.BinaryContents");
            DropTable("dbo.Messages");
        }
    }
}
