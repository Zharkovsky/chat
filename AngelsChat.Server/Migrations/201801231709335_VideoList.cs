namespace AngelsChat.Server.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class VideoList : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.VideoElements",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        InternalData = c.String(),
                        VideoId = c.Int(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Videos", t => t.VideoId)
                .Index(t => t.VideoId);
            
            AddColumn("dbo.Videos", "Date", c => c.DateTime(nullable: false));
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.VideoElements", "VideoId", "dbo.Videos");
            DropIndex("dbo.VideoElements", new[] { "VideoId" });
            DropColumn("dbo.Videos", "Date");
            DropTable("dbo.VideoElements");
        }
    }
}
