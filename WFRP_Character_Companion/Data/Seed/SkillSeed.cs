using WFRP_Character_Companion.Data;
using Microsoft.EntityFrameworkCore;
using WFRP_Character_Companion.Models;

namespace WFRP_Character_Companion.Data.Seed
{
    public class SkillSeed
    {
        public static void SeedSkills(ApplicationDbContext db)
        {
            if (db.Skills.Any()) return;

            db.Skills.AddRange(new List<Skill>
        {
            //Basic Skills
            new Skill
            {
                Name = "Atletyka",
                IsAdvanced = false,
                HasSpecialization = false,
                GoverningAttribute = AttributeType.Agility
            },

            new Skill
            {
                Name = "Broń Biała",
                IsAdvanced = false,
                HasSpecialization = true,
                GoverningAttribute = AttributeType.WeaponSkill
            },

            new Skill
            {
                Name = "Charyzma",
                IsAdvanced = false,
                HasSpecialization = false,
                GoverningAttribute = AttributeType.Fellowship
            },

            new Skill
            {
                Name = "Dowodzenie",
                IsAdvanced = false,
                HasSpecialization = false,
                GoverningAttribute = AttributeType.Fellowship
            },

            new Skill {
                Name = "Hazard",
                IsAdvanced = false,
                HasSpecialization = false,
                GoverningAttribute = AttributeType.Intelligence
            },

            new Skill {
                Name = "Intuicja",
                IsAdvanced = false,
                HasSpecialization = false,
                GoverningAttribute = AttributeType.Initiative
            },

            new Skill
            {
                Name = "Jeździectwo",
                IsAdvanced = false,
                HasSpecialization = true,
                GoverningAttribute = AttributeType.Agility
            },

            new Skill
            {
                Name = "Mocna Głowa",
                IsAdvanced = false,
                HasSpecialization = false,
                GoverningAttribute = AttributeType.Toughness
            },

            new Skill
            {
                Name = "Nawigacja",
                IsAdvanced = false,
                HasSpecialization = false,
                GoverningAttribute = AttributeType.Initiative
            },

            new Skill
            {
                Name = "Odporność",
                IsAdvanced = false,
                HasSpecialization = false,
                GoverningAttribute = AttributeType.Toughness
            },

            new Skill
            {
                Name = "Opanowanie",
                IsAdvanced = false,
                HasSpecialization = false,
                GoverningAttribute = AttributeType.Willpower
            },

            new Skill
            {
                Name = "Oswajanie",
                IsAdvanced = false,
                HasSpecialization = false,
                GoverningAttribute = AttributeType.Willpower
            },

            new Skill
            {
                Name = "Percepcja",
                IsAdvanced = false,
                HasSpecialization = false,
                GoverningAttribute = AttributeType.Initiative
            },

            new Skill
            {
                Name = "Plotkowanie",
                IsAdvanced = false,
                HasSpecialization = false,
                GoverningAttribute = AttributeType.Fellowship
            },

            new Skill
            {
                Name = "Powożenie",
                IsAdvanced = false,
                HasSpecialization = false,
                GoverningAttribute = AttributeType.Agility
            },

            new Skill
            {
                Name = "Przekupstwo",
                IsAdvanced = false,
                HasSpecialization = false,
                GoverningAttribute = AttributeType.Fellowship
            },

            new Skill
            {
                Name = "Skradanie",
                IsAdvanced = false,
                HasSpecialization = true,
                GoverningAttribute = AttributeType.Agility
            },

            new Skill
            {
                Name = "Sztuka",
                IsAdvanced = false,
                HasSpecialization = true,
                GoverningAttribute = AttributeType.Dexterity
            },

            new Skill
            {
                Name = "Sztuka Przetrwania",
                IsAdvanced = false,
                HasSpecialization = false,
                GoverningAttribute = AttributeType.Intelligence
            },

            new Skill
            {
                Name = "Targowanie",
                IsAdvanced = false,
                HasSpecialization = false,
                GoverningAttribute = AttributeType.Fellowship
            },

            new Skill
            {
                Name = "Unik",
                IsAdvanced = false,
                HasSpecialization = false,
                GoverningAttribute = AttributeType.Agility
            },

            new Skill
            {
                Name = "Wioślarstwo",
                IsAdvanced = false,
                HasSpecialization = false,
                GoverningAttribute = AttributeType.Strength
            },

            new Skill
            {
                Name = "Wspinaczka",
                IsAdvanced = false,
                HasSpecialization = false,
                GoverningAttribute = AttributeType.Strength
            },

            new Skill
            {
                Name = "Występy",
                IsAdvanced = false,
                HasSpecialization = true,
                GoverningAttribute = AttributeType.Fellowship
            },

            new Skill
            {
                Name = "Zastraszanie",
                IsAdvanced = false,
                HasSpecialization = false,
                GoverningAttribute = AttributeType.Strength
            },


            //Advanced Skills

            new Skill
            {
                Name = "Badania Naukowe",
                IsAdvanced = true,
                HasSpecialization = false,
                GoverningAttribute = AttributeType.Intelligence
            },

            new Skill
            {
                Name = "Broń Zasięgowa",
                IsAdvanced = true,
                HasSpecialization = true,
                GoverningAttribute = AttributeType.BallisticSkill
            },

            new Skill
            {
                Name = "Hipnoza",
                IsAdvanced = true,
                HasSpecialization = false,
                GoverningAttribute = AttributeType.Intelligence
            },

            new Skill
            {
                Name = "Język",
                IsAdvanced = true,
                HasSpecialization = true,
                GoverningAttribute = AttributeType.Intelligence
            },

            new Skill
            {
                Name = "Kowalstwo runiczne",
                IsAdvanced = true,
                HasSpecialization = false,
                GoverningAttribute = AttributeType.Dexterity
            },

            new Skill
            {
                Name = "Kuglarstwo",
                IsAdvanced = true,
                HasSpecialization = true,
                GoverningAttribute = AttributeType.Agility
            },

            new Skill
            {
                Name = "Leczenie",
                IsAdvanced = true,
                HasSpecialization = false,
                GoverningAttribute = AttributeType.Intelligence
            },

            new Skill
            {
                Name = "Modlitwa",
                IsAdvanced = true,
                HasSpecialization = false,
                GoverningAttribute = AttributeType.Fellowship
            },

            new Skill
            {
                Name = "Muzyka",
                IsAdvanced = true,
                HasSpecialization = true,
                GoverningAttribute = AttributeType.Dexterity
            },

            new Skill
            {
                Name = "Opieka nad Zwierzętami",
                IsAdvanced = true,
                HasSpecialization = false,
                GoverningAttribute = AttributeType.Intelligence
            },

            new Skill
            {
                Name = "Otwieranie Zamków",
                IsAdvanced = true,
                HasSpecialization = false,
                GoverningAttribute = AttributeType.Dexterity
            },

            new Skill
            {
                Name = "Pływanie",
                IsAdvanced = true,
                HasSpecialization = false,
                GoverningAttribute = AttributeType.Strength
            },

            new Skill
            {
                Name = "Psychometria",
                IsAdvanced = true,
                HasSpecialization = false,
                GoverningAttribute = AttributeType.Intelligence
            },

            new Skill
            {
                Name = "Rzemiosło",
                IsAdvanced = true,
                HasSpecialization = true,
                GoverningAttribute = AttributeType.Dexterity
            },

            new Skill
            {
                Name = "Sekretne Znaki",
                IsAdvanced = true,
                HasSpecialization = true,
                GoverningAttribute = AttributeType.Intelligence
            },

            new Skill
            {
                Name = "Splatanie Magii",
                IsAdvanced = true,
                HasSpecialization = true,
                GoverningAttribute = AttributeType.Willpower
            },

            new Skill
            {
                Name = "Tresura",
                IsAdvanced = true,
                HasSpecialization = true,
                GoverningAttribute = AttributeType.Intelligence
            },

            new Skill
            {
                Name = "Tropienie",
                IsAdvanced = true,
                HasSpecialization = false,
                GoverningAttribute = AttributeType.Initiative
            },

            new Skill
            {
                Name = "Wiedza",
                IsAdvanced = true,
                HasSpecialization = true,
                GoverningAttribute = AttributeType.Intelligence
            },

            new Skill
            {
                Name = "Wróżbiarstwo",
                IsAdvanced = true,
                HasSpecialization = false,
                GoverningAttribute = AttributeType.Intelligence
            },

            new Skill
            {
                Name = "Wycena",
                IsAdvanced = true,
                HasSpecialization = false,
                GoverningAttribute = AttributeType.Intelligence
            },

            new Skill
            {
                Name = "Zastawianie Pułapek",
                IsAdvanced = true,
                HasSpecialization = false,
                GoverningAttribute = AttributeType.Dexterity
            },

            new Skill
            {
                Name = "Zwinne Palce",
                IsAdvanced = true,
                HasSpecialization = false,
                GoverningAttribute = AttributeType.Dexterity
            },

            new Skill
            {
                Name = "Żeglarstwo",
                IsAdvanced = true,
                HasSpecialization = true,
                GoverningAttribute = AttributeType.Agility
            }
        });

            db.SaveChanges();
        }
    }
}
