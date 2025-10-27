# 🧵 BankThreads and ConcurrentLinkedList

This repository contains two independent .NET 8 Console Applications demonstrating **multithreading**, **mutex-based synchronization**, and **concurrent data structures**.

---

## 🏦 Project 1 — BankThreads

### 📘 Description
Simulates a simple **banking system** where multiple threads perform money transfers between shared accounts.  
The main goal is to demonstrate how to synchronize concurrent access to shared data using **mutexes**.

### 🧩 Features
- Multiple threads performing concurrent transfer operations.
- Two locking strategies:
  - **Global lock:** one mutex for the entire bank (simple but slow).
  - **Fine-grained lock:** per-account mutex (better performance).
- Periodic invariant checking to ensure total money consistency.
- Command-line parameters for configuration.

### ⚙️ Usage
Run from terminal:
```bash
dotnet run --project BankThreads -- -accounts 100 -threads 8 -ops 200000 -per-account
dotnet run --project BankThreads -- -accounts 100 -threads 8 -ops 200000 -default-all

dotnet run --project ConcurrentLinkedList
