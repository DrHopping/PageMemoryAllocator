using System;
using System.Collections.Generic;
using System.Linq;

namespace PageMemoryAllocator
{

    public static class ExtensionsHelpers
    {
        public static byte[] ToByteArray(this int integer) => BitConverter.GetBytes(integer);
        public static int ToInt(this byte[] array) => BitConverter.ToInt32(array);
        public static byte[] GetDescriptor(this byte[] array, int index, int descriptorSize) => array.Skip(index * descriptorSize).Take(descriptorSize).ToArray();
        public static byte[] Slice(this byte[] array, int start, int lenght) => array.Skip(start).Take(lenght).ToArray();
    }

    public class Allocator
    {
        private const int MemorySize = 2048;
        private const int PageSize = 512;
        private const int DescriptorSize = 12;

        private byte[] Desciptors { get; set; } // 0-4 - Next free block, 4-8 - Number of free blocks, 8-12 - Block size
        private byte[] Memory { get; set; }
        private Dictionary<int, List<int>> DictionarySizePages { get; set; }
        private int FreePages { get; set; }

        public Allocator()
        {
            DictionarySizePages = new Dictionary<int, List<int>>();
            FreePages = MemorySize / PageSize;
            Desciptors = new byte[MemorySize / PageSize * DescriptorSize];
            Memory = new byte[MemorySize];
        }

        
        public int mem_alloc(int allocationSize)
        {
            allocationSize = (int)Math.Pow(2, Math.Ceiling(Math.Log(allocationSize) / Math.Log(2)));


            if (allocationSize > PageSize / 2 && FreePages * PageSize >= allocationSize)
            {
                int firstEmptyPage = EmptyPage();
                for (int i = 0; i < allocationSize / PageSize; i++)
                {
                    int emptyPage = EmptyPage();
                    CreateDescriptor(emptyPage, 1, allocationSize);
                    RegisterPageInDictionary(allocationSize, emptyPage);
                    FreePages--;
                }
                return firstEmptyPage * PageSize;
            }
            else if (allocationSize > PageSize / 2 && FreePages * PageSize < allocationSize) return -1;


            var pages = DictionarySizePages.GetValueOrDefault(allocationSize);
            if(pages == null || pages.Count == 0)
            {
                int emptyPage = EmptyPage();
                if (emptyPage == -1) return -1;

                for (int i = 1; i <= PageSize / allocationSize; i++)
                {
                    var nextBlock = ((emptyPage * PageSize) + allocationSize * i).ToByteArray();
                    Array.Copy(nextBlock, 0, Memory, (emptyPage * PageSize) + allocationSize * (i - 1), nextBlock.Length);
                }
                CreateDescriptor(emptyPage, PageSize / allocationSize, allocationSize);
                RegisterPageInDictionary(allocationSize, emptyPage);
                FreePages--;
                return emptyPage * PageSize;
            } 
            else if (pages != null || pages.Count != 0)
            {
                int page = pages[0];

                int emptyBlock = Desciptors.GetDescriptor(page, DescriptorSize).Slice(0, 4).ToInt();
                int count = Desciptors.GetDescriptor(page, DescriptorSize).Slice(4, 4).ToInt();

                if(count == 1) UnregisterPageInDictionary(page);
                Array.Copy(Memory.Slice(emptyBlock, 4), 0, Desciptors, page * DescriptorSize, 4);
                AddToDescriptorCountValue(page, -1);

                return emptyBlock;
            }
            return -1;
        }
        public void mem_free(int index)
        {
            int currentPage = index / PageSize;
            var currentPageDescriptor = Desciptors.Slice(currentPage * DescriptorSize, DescriptorSize);
            int currentPageFreeBlocks = currentPageDescriptor.Slice(4, 4).ToInt();
            int currentPageBlockSize = currentPageDescriptor.Slice(8, 4).ToInt();

            if(currentPageFreeBlocks == 0 && currentPageBlockSize >= PageSize)
            {
                var pagesOfCurrentBlock = DictionarySizePages.GetValueOrDefault(currentPageBlockSize);
                for (int i = 0; i < pagesOfCurrentBlock.Count; i++)
                {
                    int pagesCount = pagesOfCurrentBlock[i];
                    Array.Copy(new byte[12], 0, Desciptors, pagesCount * DescriptorSize, DescriptorSize);
                    FreePages++;
                }
                for (int i = 0; i < pagesOfCurrentBlock.Count; i++)
                    UnregisterPageInDictionary(pagesOfCurrentBlock[0]);
            }
            else if (currentPageFreeBlocks == PageSize / currentPageBlockSize - 1)
            {
                DictionarySizePages.Remove(currentPageBlockSize);
                Array.Copy(new byte[12], 0, Desciptors, currentPage * DescriptorSize, 12);
                FreePages++;
            }
            else
            {
                var firstEmptyBlock = currentPageDescriptor.Slice(0, 4);
                var newFirstEmptyBlock = index.ToByteArray();
                Array.Copy(firstEmptyBlock, 0, Memory, index, 4);
                Array.Copy(newFirstEmptyBlock, 0, Desciptors, currentPage * DescriptorSize, 4);
                AddToDescriptorCountValue(currentPage, 1);
            }
        }
        public int mem_realloc(int size, int index)
        {
            int page = index / PageSize;
            var desciptorOfBlock = Desciptors.Slice(page * DescriptorSize, DescriptorSize);
            int blockSize = desciptorOfBlock.Slice(8, 4).ToInt();
            mem_free(index); 
            return mem_alloc(size); 
        }
        public void mem_dump()
        {
            Console.WriteLine("--------------------------------------- mem_dump ---------------------------------------");
            for (int i = 0; i < MemorySize / PageSize;  i++)
            {
                byte[] descriptor = Desciptors.Slice(i * DescriptorSize, DescriptorSize);
                int blockSize = descriptor.Slice(8, 4).ToInt();
                int count = descriptor.Slice(4, 4).ToInt();
                int blockIndex = descriptor.Slice(0, 4).ToInt();
                string pageType = blockSize == 0 ? "Free" : (blockSize <= PageSize / 2 ? "Divided" : "Multi");
                string freeBlockIndex = count == 0 && blockSize != 0 ? "None" : blockIndex.ToString();

                Console.WriteLine($"Page: {i} - BlockSize: {blockSize, 4}, Number of free blocks: {count, 2}, First Empty: {freeBlockIndex, 4}, Type: {pageType, 8}");
            }
            Console.WriteLine("----------------------------------------------------------------------------------------");
        }

     
        private void CreateDescriptor(int pageIndex, int freeBlocks, int blockSize)
        {
            int freeBlockIndex = blockSize >= PageSize ? pageIndex * PageSize : pageIndex * PageSize + blockSize;
            int startIndex = pageIndex == 0 ? 0 : pageIndex * DescriptorSize;
            Array.Copy(freeBlockIndex.ToByteArray(), 0, Desciptors, startIndex, 4);
            Array.Copy((freeBlocks - 1).ToByteArray(), 0, Desciptors, startIndex + 4, 4);
            Array.Copy(blockSize.ToByteArray(), 0, Desciptors, startIndex + 8, 4);
        }


        private int EmptyPage()
        {
            int startIndex = 8;
            for(int i = 0; i < Math.Floor((double)Desciptors.Length / DescriptorSize); i++)
            {
                int blockSize = Desciptors.Skip(startIndex).Take(4).ToArray().ToInt();
                if (blockSize == 0) return i;
                startIndex += DescriptorSize;
            }
            return -1;
        }

        private void RegisterPageInDictionary(int blockSize, int page)
        {
            var pages = DictionarySizePages.GetValueOrDefault(blockSize);
            if (pages == null)
            {
                pages = new List<int>();
                pages.Add(page);
                DictionarySizePages.Add(blockSize, pages);
            }
            else if (!pages.Contains(page)) pages.Add(page);
        }

        private void UnregisterPageInDictionary(int page)
        {
            foreach (var pair in DictionarySizePages)
                pair.Value.Remove(page);
        }

        private void AddToDescriptorCountValue(int page, int value)
        {
            var count = (Desciptors.GetDescriptor(page, DescriptorSize).Slice(4, 4).ToInt() + value).ToByteArray();
            Array.Copy(count, 0, Desciptors, page * DescriptorSize + 4, 4);
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Allocator allocator = new Allocator();
            int block1 = allocator.mem_alloc(128);
            int block2 = allocator.mem_alloc(128);
            int block3 = allocator.mem_alloc(128);
            allocator.mem_alloc(128);
            allocator.mem_alloc(128);
            allocator.mem_alloc(512);
            allocator.mem_dump();

            allocator.mem_realloc(64, block2);
            allocator.mem_dump();
        }
    }
}
