using Symbioz.ORM;
using Symbioz.Protocol.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Symbioz.Core;
using Symbioz.World.Models.Entities.Look;
using Symbioz.World.Models.Entities.Stats;
using Symbioz.World.Models.Entities.Alignment;
using Symbioz.World.Models.Entities.Jobs;
using Symbioz.World.Models.Entities;
using Symbioz.World.Models.Entities.HumanOptions;
using Symbioz.World.Models.Entities.Arena;
using Symbioz.World.Models.Entities.Shortcuts;
using Symbioz.Protocol.Enums;


namespace Symbioz.World.Records.Social
{
    [Table("Social", true, 1), Resettable]
    public class SocialRecord : ITable
    {
        public static List<SocialRecord> Social = new List<SocialRecord>();

        [Primary]
        public long Id; 

        [Update]
        public long AccountId;        

        [Update]
        public long FriendAccountId;

        [Update]
        public string FriendName;
        
           

        public SocialRecord(long id,long accountId,long friendAccountId, string FriendName)
        {
            this.Id = id;
            this.AccountId = accountId;
            this.FriendAccountId = friendAccountId;
            this.FriendName = FriendName;

        }
          public static List<SocialRecord> GetSocialByAccountId(int accountId)
        {
            return Social.FindAll(x => x.AccountId == accountId);
        } 
         public static bool CheckSocial(int accountId, int friendAccountId)
        {
            return Social.Any(x => x.AccountId == accountId && x.FriendAccountId == friendAccountId);
        } 

       

    }
}