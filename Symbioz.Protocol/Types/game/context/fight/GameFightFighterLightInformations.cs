


















// Generated on 04/27/2016 01:13:12
using System;
using System.Collections.Generic;
using System.Linq;
using SSync.IO;

namespace Symbioz.Protocol.Types
{

public class GameFightFighterLightInformations
{

public const short Id = 413;
public virtual short TypeId
{
    get { return Id; }
}

public bool sex;
        public bool alive;
        public double id;
        public sbyte wave;
        public ushort level;
        public sbyte breed;
        

public GameFightFighterLightInformations()
{
}

public GameFightFighterLightInformations(bool sex, bool alive, double id, sbyte wave, ushort level, sbyte breed)
        {
            this.sex = sex;
            this.alive = alive;
            this.id = id;
            this.wave = wave;
            this.level = level;
            this.breed = breed;
        }
        

public virtual void Serialize(ICustomDataOutput writer)
{

byte flag1 = 0;
            flag1 = BooleanByteWrapper.SetFlag(flag1, 0, sex);
            flag1 = BooleanByteWrapper.SetFlag(flag1, 1, alive);
            writer.WriteByte(flag1);
            writer.WriteDouble(id);
            writer.WriteByte((byte)wave);
            writer.WriteVarUhShort(level);
            writer.WriteByte((byte)breed);
            

}

public virtual void Deserialize(ICustomDataInput reader)
{

byte flag1 = reader.ReadByte();
            sex = BooleanByteWrapper.GetFlag(flag1, 0);
            alive = BooleanByteWrapper.GetFlag(flag1, 1);
            id = reader.ReadDouble();
            if (id < -9007199254740990 || id > 9007199254740990)
                throw new Exception("Forbidden value on id = " + id + ", it doesn't respect the following condition : id < -9007199254740990 || id > 9007199254740990");
            wave = (sbyte)reader.ReadByte();
            if (wave < 0)
                throw new Exception("Forbidden value on wave = " + wave + ", it doesn't respect the following condition : wave < 0");
            level = reader.ReadVarUhShort();
            if (level < 0)
                throw new Exception("Forbidden value on level = " + level + ", it doesn't respect the following condition : level < 0");
            breed = (sbyte)reader.ReadByte();
            

}


}


}