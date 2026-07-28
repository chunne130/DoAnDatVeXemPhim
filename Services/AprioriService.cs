using System;
using System.Collections.Generic;
using System.Linq;

namespace DoAnDatVeXemPhim.Services
{
    public class ItemSet<T> : IEquatable<ItemSet<T>> where T : IComparable<T>
    {
        public SortedSet<T> Items { get; }
        public double Support { get; set; }

        public ItemSet(IEnumerable<T> items, double support = 0)
        {
            Items = new SortedSet<T>(items);
            Support = support;
        }

        public bool Equals(ItemSet<T>? other)
        {
            if (other == null) return false;
            return Items.SetEquals(other.Items);
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as ItemSet<T>);
        }

        public override int GetHashCode()
        {
            int hash = 19;
            foreach (var item in Items)
            {
                hash = hash * 31 + (item?.GetHashCode() ?? 0);
            }
            return hash;
        }

        public override string ToString()
        {
            return string.Join(",", Items);
        }
    }

    public class AprioriRule<T> where T : IComparable<T>
    {
        public SortedSet<T> Antecedent { get; set; }
        public SortedSet<T> Consequent { get; set; }
        public double Support { get; set; }
        public double Confidence { get; set; }
        public double Lift { get; set; }

        public AprioriRule(SortedSet<T> antecedent, SortedSet<T> consequent, double support, double confidence, double lift)
        {
            Antecedent = antecedent;
            Consequent = consequent;
            Support = support;
            Confidence = confidence;
            Lift = lift;
        }
    }

    public class AprioriService
    {
        /// <summary>
        /// Chạy thuật toán Apriori để tìm các luật kết hợp.
        /// </summary>
        /// <typeparam name="T">Kiểu dữ liệu của các phần tử (phải so sánh được)</typeparam>
        /// <param name="transactions">Danh sách các giao dịch, mỗi giao dịch là tập các phần tử</param>
        /// <param name="minSupport">Độ hỗ trợ tối thiểu (từ 0.0 đến 1.0)</param>
        /// <param name="minConfidence">Độ tin cậy tối thiểu (từ 0.0 đến 1.0)</param>
        /// <returns>Danh sách luật kết hợp phát hiện được</returns>
        public List<AprioriRule<T>> Run<T>(List<List<T>> transactions, double minSupport, double minConfidence) where T : IComparable<T>
        {
            if (transactions == null || transactions.Count == 0)
                return new List<AprioriRule<T>>();

            int totalTransactions = transactions.Count;

            // 1. Tìm các tập phổ biến (Frequent Itemsets)
            var frequentItemsets = FindFrequentItemsets(transactions, minSupport);

            // 2. Sinh luật kết hợp từ các tập phổ biến
            var rules = GenerateAssociationRules(transactions, frequentItemsets, minConfidence);

            return rules;
        }

        // hàm đếm tần suất theo tiêu chí minSupport
        private List<ItemSet<T>> FindFrequentItemsets<T>(List<List<T>> transactions, double minSupport) where T : IComparable<T>
        {
            var allFrequentItemsets = new List<ItemSet<T>>();
            int totalTransactions = transactions.Count;

            // --- BƯỚC 1: Tìm L1 (các tập phổ biến kích thước 1) ---
            var itemCounts = new Dictionary<T, int>();
            foreach (var transaction in transactions)
            {
                var uniqueItems = transaction.Distinct();
                foreach (var item in uniqueItems)
                {
                    if (itemCounts.ContainsKey(item))
                        itemCounts[item]++;
                    else
                        itemCounts[item] = 1;
                }
            }

            var L1 = new List<ItemSet<T>>();
            foreach (var kvp in itemCounts)
            {
                double support = (double)kvp.Value / totalTransactions;
                if (support >= minSupport)
                {
                    L1.Add(new ItemSet<T>(new[] { kvp.Key }, support));
                }
            }

            allFrequentItemsets.AddRange(L1);

            // --- BƯỚC 2: Tìm L_k (các tập phổ biến kích thước k từ 2 trở đi) ---
            var currentL = L1;
            int k = 2;

            while (currentL.Count > 0)
            {
                // Sinh ứng viên C_k
                var candidates = GenerateCandidates(currentL, k);
                if (candidates.Count == 0)
                    break;

                // Đếm độ hỗ trợ thực tế
                var candidateCounts = new Dictionary<ItemSet<T>, int>();
                foreach (var candidate in candidates)
                {
                    candidateCounts[candidate] = 0;
                }

                foreach (var transaction in transactions)
                {
                    var transactionSet = new HashSet<T>(transaction);
                    foreach (var candidate in candidates)
                    {
                        if (candidate.Items.All(item => transactionSet.Contains(item)))
                        {
                            candidateCounts[candidate]++;
                        }
                    }
                }

                // Lọc ứng viên vượt qua minSupport để tạo L_k
                var nextL = new List<ItemSet<T>>();
                foreach (var kvp in candidateCounts)
                {
                    double support = (double)kvp.Value / totalTransactions;
                    if (support >= minSupport)
                    {
                        kvp.Key.Support = support;
                        nextL.Add(kvp.Key);
                    }
                }

                if (nextL.Count == 0)
                    break;

                allFrequentItemsets.AddRange(nextL);
                currentL = nextL;
                k++;
            }

            return allFrequentItemsets;
        }

        // hàm ghép các sản phẩm 
        private List<ItemSet<T>> GenerateCandidates<T>(List<ItemSet<T>> previousFrequent, int k) where T : IComparable<T>
        {
            var candidates = new List<ItemSet<T>>();
            int count = previousFrequent.Count;

            // Join Step: Kết hợp các tập phổ biến kích thước k-1 với nhau
            for (int i = 0; i < count; i++)
            {
                var list1 = previousFrequent[i].Items.ToList();
                for (int j = i + 1; j < count; j++)
                {
                    var list2 = previousFrequent[j].Items.ToList();

                    // Hai tập chỉ join được nếu k-2 phần tử đầu giống nhau
                    bool canJoin = true;
                    for (int n = 0; n < k - 2; n++)
                    {
                        if (list1[n].CompareTo(list2[n]) != 0)
                        {
                            canJoin = false;
                            break;
                        }
                    }

                    if (canJoin)
                    {
                        // Thêm phần tử cuối của list2 vào list1
                        var candidateItems = new SortedSet<T>(previousFrequent[i].Items)
                        {
                            list2[k - 2]
                        };

                        var candidate = new ItemSet<T>(candidateItems);

                        // Pruning Step: Cắt tỉa nếu tập con kích thước k-1 của nó không phổ biến
                        if (!HasInfrequentSubset(candidate, previousFrequent))
                        {
                            if (!candidates.Any(c => c.Equals(candidate)))
                            {
                                candidates.Add(candidate);
                            }
                        }
                    }
                }
            }

            return candidates;
        }

        // hàm lọc chặn sản phẩm ế 
        private bool HasInfrequentSubset<T>(ItemSet<T> candidate, List<ItemSet<T>> previousFrequent) where T : IComparable<T>
        {
            var items = candidate.Items.ToList();
            int k = items.Count;

            // Tạo các tập con kích thước k-1 bằng cách loại bỏ từng phần tử một
            for (int i = 0; i < k; i++)
            {
                var subsetItems = new SortedSet<T>(items);
                subsetItems.Remove(items[i]);

                var subset = new ItemSet<T>(subsetItems);

                // Kiểm tra xem tập con này có nằm trong previousFrequent không
                bool found = false;
                foreach (var freq in previousFrequent)
                {
                    if (freq.Equals(subset))
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                    return true; // Có tập con không phổ biến -> Cần cắt tỉa ứng viên này
            }

            return false;
        }

        private List<AprioriRule<T>> GenerateAssociationRules<T>(List<List<T>> transactions, List<ItemSet<T>> frequentItemsets, double minConfidence) where T : IComparable<T>
        {
            var rules = new List<AprioriRule<T>>();
            int totalTransactions = transactions.Count;

            // Lưu trữ độ hỗ trợ của mọi tập phổ biến để tra cứu nhanh hơn
            var supportLookup = new Dictionary<string, double>();
            foreach (var itemset in frequentItemsets)
            {
                supportLookup[itemset.ToString()] = itemset.Support;
            }

            // Chỉ xét các tập phổ biến có kích thước từ 2 trở lên
            foreach (var itemset in frequentItemsets.Where(it => it.Items.Count >= 2))
            {
                var itemList = itemset.Items.ToList();
                var subsets = GetSubsets(itemList);

                foreach (var antecedentList in subsets)
                {
                    var antecedentSet = new SortedSet<T>(antecedentList);
                    var consequentSet = new SortedSet<T>(itemset.Items.Except(antecedentSet));

                    var antKey = string.Join(",", antecedentSet);
                    var consKey = string.Join(",", consequentSet);

                    if (supportLookup.TryGetValue(antKey, out double antecedentSupport))
                    {
                        double support = itemset.Support;
                        double confidence = support / antecedentSupport;

                        if (confidence >= minConfidence)
                        {
                            // Tính toán Lift
                            double consequentSupport = 0;
                            if (supportLookup.TryGetValue(consKey, out double cs))
                            {
                                consequentSupport = cs;
                            }
                            else
                            {
                                // Đếm support thực tế cho Consequent nếu nó không nằm trong danh sách tập phổ biến
                                int count = transactions.Count(t => consequentSet.All(item => t.Contains(item)));
                                consequentSupport = (double)count / totalTransactions;
                            }

                            double lift = confidence / consequentSupport;

                            rules.Add(new AprioriRule<T>(
                                antecedentSet,
                                consequentSet,
                                support,
                                confidence,
                                lift
                            ));
                        }
                    }
                }
            }

            return rules;
        }

        private List<List<T>> GetSubsets<T>(List<T> items)
        {
            var subsets = new List<List<T>>();
            int subsetCount = 1 << items.Count;

            // Đi từ 1 đến subsetCount - 2 để loại bỏ tập rỗng (0) và tập cha đầy đủ (subsetCount - 1)
            for (int i = 1; i < subsetCount - 1; i++)
            {
                var subset = new List<T>();
                for (int j = 0; j < items.Count; j++)
                {
                    if ((i & (1 << j)) > 0)
                    {
                        subset.Add(items[j]);
                    }
                }
                subsets.Add(subset);
            }

            return subsets;
        }
    }
}
