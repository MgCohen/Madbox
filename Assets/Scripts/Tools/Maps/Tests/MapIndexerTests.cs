using System.Collections.Generic;
using System;
using NUnit.Framework;

namespace Scaffold.Maps.Tests
{
    public class MapIndexerTests
    {
        [Test]
        public void AddIndexer_PopulatesIndexerFromExistingEntries()
        {
            Map<string, int, string> map = CreateMapWithPeople();
            Indexer<string, int, string> indexer = map.AddIndexer("MatheusAdults", BuildMatchesMatheusAdult);
            int count = indexer.Count;
            bool hasAdult = BuildContainsValue(indexer.Values, "Matheus-29");
            Assert.AreEqual(1, count);
            Assert.IsTrue(hasAdult);
        }

        [Test]
        public void Add_MatchingEntryAfterIndexerRegistration_AddsEntryToIndexer()
        {
            Map<string, int, string> map = new Map<string, int, string>();
            Indexer<string, int, string> indexer = map.AddIndexer("MatheusAdults", BuildMatchesMatheusAdult);
            map.Add("Matheus", 29, "Matheus-29");
            map.Add("Ana", 30, "Ana-30");
            int count = indexer.Count;
            Assert.AreEqual(1, count);
        }

        [Test]
        public void Remove_RemovesEntryFromIndexer()
        {
            Map<string, int, string> map = new Map<string, int, string>();
            Indexer<string, int, string> indexer = map.AddIndexer("MatheusAdults", BuildMatchesMatheusAdult);
            map.Add("Matheus", 29, "Matheus-29");
            bool wasRemoved = map.Remove("Matheus", 29);
            int count = indexer.Count;
            Assert.IsTrue(wasRemoved);
            Assert.AreEqual(0, count);
        }

        [Test]
        public void SetByIndex_DoesNotRemoveFromIndexerWhenValueChanges()
        {
            Map<string, int, string> map = new Map<string, int, string>();
            Indexer<string, int, string> indexer = map.AddIndexer("MatheusAdults", BuildMatchesMatheusAdult);
            map.Add("Matheus", 29, "Matheus-29");
            Index<string, int> index = new Index<string, int>("Matheus", 29);
            BuildAssertIndexerCountUnchangedByValueChange(map, indexer, index);
        }

        [Test]
        public void Clear_RemovesAllIndexedValues()
        {
            Map<string, int, string> map = CreateMapWithPeople();
            Indexer<string, int, string> indexer = map.AddIndexer("MatheusAdults", BuildMatchesMatheusAdult);
            map.Clear();
            int count = indexer.Count;
            Assert.AreEqual(0, count);
        }

        [Test]
        public void TryGetIndexer_WhenIndexerMissing_ReturnsFalse()
        {
            Map<string, int, string> map = new Map<string, int, string>();
            bool found = map.TryGetIndexer("missing", out Indexer<string, int, string> indexer);
            Assert.IsFalse(found);
            Assert.IsNull(indexer);
        }

        [Test]
        public void AddIndexer_WithNullPredicate_ThrowsArgumentNullException()
        {
            Map<string, int, string> map = new Map<string, int, string>();
            TestDelegate action = () => map.AddIndexer("MatheusAdults", null);
            Assert.Throws<ArgumentNullException>(action);
        }

        [Test]
        public void AddIndexer_WithDuplicateName_ThrowsArgumentException()
        {
            Map<string, int, string> map = new Map<string, int, string>();
            map.AddIndexer("MatheusAdults", BuildMatchesMatheusAdult);
            TestDelegate action = () => map.AddIndexer("MatheusAdults", BuildMatchesMatheusAdult);
            Assert.Throws<ArgumentException>(action);
        }

        [Test]
        public void GetIndexedValues_WithEmptyName_ThrowsArgumentException()
        {
            Map<string, int, string> map = new Map<string, int, string>();
            TestDelegate action = () => map.GetIndexedValues("");
            Assert.Throws<ArgumentException>(action);
        }

        private static Map<string, int, string> CreateMapWithPeople()
        {
            Map<string, int, string> map = new Map<string, int, string>();
            map.Add("Matheus", 9, "Matheus-9");
            map.Add("Matheus", 29, "Matheus-29");
            map.Add("Ana", 29, "Ana-29");
            return map;
        }

        private static bool BuildMatchesMatheusAdult(string name, int age)
        {
            return name == "Matheus" && age > 10;
        }

        private static bool BuildContainsValue(IReadOnlyCollection<string> values, string expected)
        {
            HashSet<string> set = new HashSet<string>(values);
            bool hasValue = set.Contains(expected);
            return hasValue;
        }

        private static void BuildAssertIndexerCountUnchangedByValueChange(Map<string, int, string> map, Indexer<string, int, string> indexer, Index<string, int> index)
        {
            map[index] = "inactive";
            int countAfterInactive = indexer.Count;
            map[index] = "active";
            int countAfterActive = indexer.Count;
            Assert.AreEqual(1, countAfterInactive);
            Assert.AreEqual(1, countAfterActive);
        }
    }
}


