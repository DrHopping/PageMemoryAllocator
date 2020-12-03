# Page Memory Allocator
Here is presented my C# implementation of **Page Memory Allocator** . 
Memoty in the allocator divided into serveral types of pages. All pages have the same size. Number of pages can be calculated by formula ``MemorySize / PageSize``
**Descriptor** is a sctructure that stores information about pages. Descriptor consists of 12 bytes.
- [0-4] - index of next free block
- [4-8] - number of free blocks
- [8-12] - size of block

Pages is divided into 2 types:
- Pages that divided into blocs of the same size
- Pages that occupied by block that bigger than half of the page

First type of pages divided into classes. Class determine what size of blocks can be stored in the page.
If desired allocation size is not a power of 2, its casts to nearest power of two that bigger than it.

 
## Examples
#### Allocation 
Allocate block with ``AllocationSize < PageSize / 2``

```
Allocator allocator = new Allocator();
allocator.mem_alloc(128);
allocator.mem_alloc(128);
allocator.mem_alloc(128);
allocator.mem_alloc(128);
allocator.mem_alloc(64);
allocator.mem_alloc(64);
allocator.mem_dump();
```

![alt text](img/1.png)   

Allocate block with ``AllocationSize > PageSize / 2``
```
Allocator allocator = new Allocator();
allocator.mem_alloc(1024);
allocator.mem_alloc(512);
allocator.mem_dump();

```

![alt text](img/2.png)   


#### Memory Free
 Allocate
```
Allocator allocator = new Allocator();
int block1 = allocator.mem_alloc(128);
int block2 = allocator.mem_alloc(128);
int block3 = allocator.mem_alloc(128);
allocator.mem_alloc(64);
allocator.mem_alloc(64);
allocator.mem_alloc(512);
allocator.mem_dump();
```
![alt text](img/3.png)   

Memory free : 
```
allocator.mem_free(ind1);
allocator.mem_free(ind2);
allocator.mem_free(ind3);
allocator.mem_free(ind4);
```

![alt text](img/4.png)   

#### Realloc

Allocate
```
Allocator allocator = new Allocator();
int block1 = allocator.mem_alloc(128);
int block2 = allocator.mem_alloc(128);
int block3 = allocator.mem_alloc(128);
allocator.mem_alloc(64);
allocator.mem_alloc(64);
allocator.mem_alloc(512);
allocator.mem_dump();
```
![alt text](img/3.png)   

Reallocate
```
allocator.mem_realloc(64, block2);
allocator.mem_dump();
```
![alt text](img/5.png)   
