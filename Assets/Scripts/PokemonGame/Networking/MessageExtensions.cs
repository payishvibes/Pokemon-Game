using System.Collections.Generic;
using System.Linq;
using PokemonGame.Game.Party;
using Riptide;
using PokemonGame.General;
using PokemonGame.ScriptableObjects;
using UnityEngine;

public static class MessageExtensions
{
   public static Message Add(this Message message, Party value) => AddParty(message, value);
   
    public static Message AddParty(this Message message, Party value)
    {
        message.AddInt(value.Count);
        
        for (int i = 0; i < value.Count; i++)
        {
            message.AddBattler(value[i]);
        }
        
        return message;
    }
    
    public static Party GetParty(this Message message)
    {
        Debug.Log(message.BytesInUse);
        
        Party party = new Party();
        int size = message.GetInt();
        
        for (int i = 0; i < size; i++)
        {
            Debug.Log(i);
            Battler battler = message.GetBattler();
            party.Add(battler);
        }
        
        return party;
    }
    
    public static Message AddBattler(this Message message, Battler value)
    {
        message.AddString(value.source.name);
        message.AddInt(value.level);
        message.AddInt((int)value.gender);
        message.AddBattlerStats(value.IVs);
        message.AddInt((int)value.nature);

        message.AddInt(value.moves.Count);
        for (int i =0; i < value.moves.Count; i++)
        {
            message.AddString(value.moves[i].type.name);
        }
        
        for (int i =0; i < value.moves.Count; i++)
        {
            message.AddString(value.moves[i].name);
        }
        
        return message;
    }

    public static Battler GetBattler(this Message message)
    {
        Battler returnBattler = ScriptableObject.CreateInstance<Battler>();
        
        string sourceName = message.GetString();
        int level = message.GetInt();
        Gender gender = (Gender)message.GetInt();
        BattlerStats IVs = message.GetBattlerStats();
        Nature nature = (Nature)message.GetInt();
        int length = message.GetInt();
        List<string> moveTypes = new List<string>();
        for (int i = 0; i < length; i++)
        {
            moveTypes.Add(message.GetString());
        }
        List<string> moveNames = new List<string>();
        for (int i = 0; i < length; i++)
        {
            moveNames.Add(message.GetString());
        }
        
        returnBattler.source = Resources.Load<BattlerTemplate>($"Pokemon Game/Battler Template/{sourceName}");
        returnBattler.UpdateLevel(level);
        returnBattler.name = sourceName;
        returnBattler.isFainted = false;
        returnBattler.exp = 0;
        returnBattler.statusEffect = StatusEffect.Healthy;
        returnBattler.moves = new List<Move>();
        returnBattler.movePpInfos = new List<MovePPData>();
        returnBattler.EVs = BattlerStats.zero;
        returnBattler.shiny = false;
        returnBattler.gender = gender;
        returnBattler.IVs = IVs;
        returnBattler.nature = nature;

        for (int i = 0; i < moveNames.Count; i++)
        {
            returnBattler.LearnMove(Resources.Load<Move>($"Pokemon Game/Move/{moveTypes[i]}/{moveNames[i]}"));
        }
        
        returnBattler.UpdateStats();

        returnBattler.currentHealth = returnBattler.stats.maxHealth;
        
        return returnBattler;
    }
    
    public static Message AddBattlerStats(this Message message, BattlerStats value)
    {
        message.AddInt(value.maxHealth);
        message.AddInt(value.attack);
        message.AddInt(value.defense);
        message.AddInt(value.specialAttack);
        message.AddInt(value.specialDefense);
        message.AddInt(value.speed);
        
        return message;
    }

    public static BattlerStats GetBattlerStats(this Message message)
    {
        BattlerStats returnBattlerStats = new BattlerStats();
        returnBattlerStats.maxHealth = message.GetInt();
        returnBattlerStats.attack = message.GetInt();
        returnBattlerStats.defense = message.GetInt();
        returnBattlerStats.specialAttack = message.GetInt();
        returnBattlerStats.specialDefense = message.GetInt();
        returnBattlerStats.speed = message.GetInt();

        return returnBattlerStats;
    }
}
