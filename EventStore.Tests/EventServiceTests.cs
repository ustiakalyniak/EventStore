using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using EventStore.BLL.Services;
using EventStore.DAL.Repositories;
using EventStore.DAL.Models;

namespace EventStore.Tests
{
    public class EventServiceTests
    {
        private readonly Mock<IEventRepository> _mockRepo;
        private readonly Mock<ILogger<EventService>> _mockLogger;
        private readonly EventService _service;

        public EventServiceTests()
        {
            _mockRepo = new Mock<IEventRepository>();
            _mockLogger = new Mock<ILogger<EventService>>();
            _service = new EventService(_mockRepo.Object, _mockLogger.Object);
        }

        // =========================================================================
        // ЮЗ-КЕЙС 1, 3: БАЗОВІ ОПЕРАЦІЇ З ПОДІЯМИ
        // =========================================================================

        [Fact]
        public async Task CreateEventAsync_WhenBudgetIsInvalid_ShouldReturnFailure()
        {
            var invalidEvent = new Event { Name = "Тест", Budget = -500, GuestsCount = 10 };
            var result = await _service.CreateEventAsync(invalidEvent);

            Assert.False(result.IsSuccess);
            Assert.Equal("Бюджет події не може бути від'ємним.", result.ErrorMessage);
            _mockRepo.Verify(r => r.AddAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task CreateEventAsync_WhenDataIsValid_ShouldSaveToDatabase()
        {
            var validEvent = new Event { Name = "Корпоратив", Budget = 50000, GuestsCount = 20 };
            _mockRepo.Setup(r => r.AddAsync(validEvent, It.IsAny<CancellationToken>())).ReturnsAsync(validEvent);

            var result = await _service.CreateEventAsync(validEvent);

            Assert.True(result.IsSuccess);
            _mockRepo.Verify(r => r.AddAsync(validEvent, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task DeleteEventAsync_WhenEventExists_ShouldCallDeleteOnRepository()
        {
            int eventIdToDelete = 1;
            _mockRepo.Setup(r => r.DeleteAsync(eventIdToDelete, It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var result = await _service.DeleteEventAsync(eventIdToDelete);

            Assert.True(result.IsSuccess);
            _mockRepo.Verify(r => r.DeleteAsync(eventIdToDelete, It.IsAny<CancellationToken>()), Times.Once);
        }

        // Негативні тести для подій
        [Fact]
        public async Task CreateEventAsync_WhenEventIsNull_ShouldReturnFailure()
        {
            var result = await _service.CreateEventAsync(null!);

            Assert.False(result.IsSuccess);
            Assert.Equal("Дані події відсутні.", result.ErrorMessage);
            _mockRepo.Verify(r => r.AddAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task CreateEventAsync_WhenNameIsEmpty_ShouldReturnFailure()
        {
            var invalidEvent = new Event { Name = "   ", Budget = 10000 };
            var result = await _service.CreateEventAsync(invalidEvent);

            Assert.False(result.IsSuccess);
            Assert.Equal("Назва події не може бути порожньою.", result.ErrorMessage);
        }

        [Fact]
        public async Task DeleteEventAsync_WhenIdIsInvalid_ShouldReturnFailure()
        {
            var result = await _service.DeleteEventAsync(0);

            Assert.False(result.IsSuccess);
            Assert.Equal("ID події має бути більше нуля.", result.ErrorMessage);
            _mockRepo.Verify(r => r.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task DeleteEventAsync_WhenEventNotFound_ShouldReturnFailure()
        {
            int nonExistentId = 999;
            _mockRepo.Setup(r => r.DeleteAsync(nonExistentId, It.IsAny<CancellationToken>())).ReturnsAsync(false);
            var result = await _service.DeleteEventAsync(nonExistentId);

            Assert.False(result.IsSuccess);
            Assert.Equal("Подію не знайдено.", result.ErrorMessage);
        }

        // =========================================================================
        // ЮЗ-КЕЙС 7: ГЕНЕРАЦІЯ ПЛАНУ (GetOrGeneratePlanAsync)
        // =========================================================================

        [Fact]
        public async Task GetOrGeneratePlanAsync_WhenPlanExists_ShouldReturnExistingItems_WithoutGenerating()
        {
            int eventId = 1;
            var existingItems = new List<PlanItem> { new PlanItem { Title = "Існуючий пункт" } };
            _mockRepo.Setup(r => r.GetPlanItemsByEventIdAsync(eventId, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(existingItems);

            var result = await _service.GetOrGeneratePlanAsync(eventId);

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Single(result.Data!);
            
            _mockRepo.Verify(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task GetOrGeneratePlanAsync_WhenNoPlanExists_ShouldGenerateBasedOnWishes()
        {
            int eventId = 1;
            var newEvent = new Event { Id = 1, Wishes = "Локація,Музика", Budget = 10000 };
            
            _mockRepo.Setup(r => r.GetPlanItemsByEventIdAsync(eventId, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(new List<PlanItem>()); 
            _mockRepo.Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(newEvent);

            var result = await _service.GetOrGeneratePlanAsync(eventId);

            Assert.True(result.IsSuccess);
            _mockRepo.Verify(r => r.AddPlanItemsAsync(It.IsAny<List<PlanItem>>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        // =========================================================================
        // ЮЗ-КЕЙС 8: ЗМІНА СТАТУСУ (UpdatePlanItemStatusAndRecalculateAsync)
        // =========================================================================

        [Fact]
        public async Task UpdatePlanItemStatusAndRecalculateAsync_ShouldUpdateStatus_AndTriggerRecalculation()
        {
            int eventId = 1;
            int taskId = 5;
            string newStatus = "InProgress";

            _mockRepo.Setup(r => r.GetPlanItemsByEventIdAsync(eventId, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(new List<PlanItem>());
            _mockRepo.Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(new Event { Id = eventId });

            var result = await _service.UpdatePlanItemStatusAndRecalculateAsync(eventId, taskId, newStatus);

            Assert.True(result.IsSuccess);
            _mockRepo.Verify(r => r.UpdatePlanItemStatusAsync(taskId, newStatus, It.IsAny<CancellationToken>()), Times.Once);
        }

        // =========================================================================
        // ЮЗ-КЕЙС 9: ДИНАМІЧНИЙ ПЕРЕРАХУНОК (RecalculateEventProgressAsync)
        // =========================================================================

        [Fact]
        public async Task RecalculateEventProgressAsync_ShouldCalculateCorrectPercentage_BasedOnStatusWeights()
        {
            int eventId = 1;
            var testEvent = new Event { Id = eventId };
            
            var planItems = new List<PlanItem>
            {
                new PlanItem { Status = "Completed" },  
                new PlanItem { Status = "InProgress" }, 
                new PlanItem { Status = "NotStarted" }, 
                new PlanItem { Status = "NotStarted" }   
            };
            _mockRepo.Setup(r => r.GetPlanItemsByEventIdAsync(eventId, It.IsAny<CancellationToken>())).ReturnsAsync(planItems);
            _mockRepo.Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>())).ReturnsAsync(testEvent);

            var result = await _service.RecalculateEventProgressAsync(eventId);

            Assert.True(result.IsSuccess);
            Assert.Equal(38, result.Data); 
            _mockRepo.Verify(r => r.UpdateAsync(testEvent, It.IsAny<CancellationToken>()), Times.Once); 
        }

        // =========================================================================
        // ЮЗ-КЕЙС 10: КЕРУВАННЯ СПИСКОМ ГОСТЕЙ ТА RSVP (Guest List)
        // =========================================================================

        [Fact]
        public async Task AddGuestAsync_WhenGuestLimitIsReached_ShouldReturnFailure()
        {
            var eventMock = new Event { Id = 1, GuestsCount = 2 }; 
            var currentGuests = new List<Guest>
            {
                new Guest { Id = 1, Name = "Іван", EventId = 1 },
                new Guest { Id = 2, Name = "Марія", EventId = 1 }
            };

            _mockRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(eventMock);
            _mockRepo.Setup(r => r.GetGuestsByEventIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(currentGuests);

            var extraGuest = new Guest { Name = "Олег", EventId = 1 };
            
            var result = await _service.AddGuestAsync(1, extraGuest);
            
            Assert.False(result.IsSuccess);
            Assert.Contains("Перевищено ліміт", result.ErrorMessage);
            _mockRepo.Verify(r => r.AddGuestAsync(It.IsAny<Guest>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task AddGuestAsync_WhenUnderLimit_ShouldSaveToDatabase()
        {
            var eventMock = new Event { Id = 1, GuestsCount = 5 }; 
            var currentGuests = new List<Guest> { new Guest { Id = 1, Name = "Іван" } };

            _mockRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(eventMock);
            _mockRepo.Setup(r => r.GetGuestsByEventIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(currentGuests);

            var newGuest = new Guest { Name = "Марія", EventId = 1 };
            
            var result = await _service.AddGuestAsync(1, newGuest);
            
            Assert.True(result.IsSuccess);
            _mockRepo.Verify(r => r.AddGuestAsync(newGuest, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateGuestStatusAsync_ShouldCallRepository_WithCorrectData()
        {
            int guestId = 10;
            string expectedStatus = "Confirmed"; 
            
            var result = await _service.UpdateGuestStatusAsync(guestId, expectedStatus);
            
            Assert.True(result.IsSuccess);
            _mockRepo.Verify(r => r.UpdateGuestStatusAsync(guestId, expectedStatus, It.IsAny<CancellationToken>()), Times.Once);
        }

        // Негативні тести для гостів
        [Fact]
        public async Task AddGuestAsync_WhenEventNotFound_ShouldReturnFailure()
        {
            int eventId = 99;
            _mockRepo.Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>())).ReturnsAsync((Event?)null);

            var guest = new Guest { Name = "Олена" };
            var result = await _service.AddGuestAsync(eventId, guest);

            Assert.False(result.IsSuccess);
            Assert.Equal("Подію не знайдено.", result.ErrorMessage);
        }

        [Fact]
        public async Task DeleteGuestAsync_WhenGuestNotFound_ShouldReturnFailure()
        {
            int guestId = 999;
            _mockRepo.Setup(r => r.DeleteGuestAsync(guestId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

            var result = await _service.DeleteGuestAsync(guestId);
            Assert.False(result.IsSuccess);
            Assert.Equal("Гостя не знайдено для видалення.", result.ErrorMessage);
        }

        // =========================================================================
        // ЮЗ-КЕЙС 11: ФІНАНСОВИЙ ОБЛІК (Expense Tracking)
        // =========================================================================

        [Fact]
        public async Task AddExpenseTransactionAsync_WhenAmountIsInvalid_ShouldReturnFailure()
        {
            var result = await _service.AddExpenseTransactionAsync(1, -500, "Тест");
            
            Assert.False(result.IsSuccess);
            Assert.Equal("Сума оплати має бути більшою за нуль.", result.ErrorMessage);
            _mockRepo.Verify(r => r.AddTransactionAsync(It.IsAny<ExpenseTransaction>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task AddExpenseTransactionAsync_WhenValid_ShouldSaveToDatabase()
        {
            int planItemId = 5;
            decimal amount = 1000m;
            var expectedTransaction = new ExpenseTransaction { PlanItemId = planItemId, Amount = amount };
            
            _mockRepo.Setup(r => r.AddTransactionAsync(It.IsAny<ExpenseTransaction>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(expectedTransaction);

            var result = await _service.AddExpenseTransactionAsync(planItemId, amount, "Аванс");

            Assert.True(result.IsSuccess);
            _mockRepo.Verify(r => r.AddTransactionAsync(It.IsAny<ExpenseTransaction>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetEventFinancialBalanceAsync_ShouldCalculateCorrectly()
        {
            int eventId = 1;
            var ev = new Event { Id = eventId, Budget = 10000m }; 
            var planItems = new List<PlanItem>
            {
                new PlanItem { Id = 1, Cost = 3000m },
                new PlanItem { Id = 2, Cost = 5000m }
            }; 
            
            var transactions = new List<ExpenseTransaction>
            {
                new ExpenseTransaction { PlanItemId = 1, Amount = 1000m },
                new ExpenseTransaction { PlanItemId = 2, Amount = 2000m }
            }; 

            _mockRepo.Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>())).ReturnsAsync(ev);
            _mockRepo.Setup(r => r.GetPlanItemsByEventIdAsync(eventId, It.IsAny<CancellationToken>())).ReturnsAsync(planItems);
            _mockRepo.Setup(r => r.GetTransactionsByEventIdAsync(eventId, It.IsAny<CancellationToken>())).ReturnsAsync(transactions);

            var result = await _service.GetEventFinancialBalanceAsync(eventId);

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            
            Assert.Equal(10000m, result.Data!.TotalBudget);
            Assert.Equal(8000m, result.Data!.PlannedCost);
            Assert.Equal(3000m, result.Data!.ActuallyPaid);
            Assert.Equal(5000m, result.Data!.RemainingToPay); 
            Assert.Equal(2000m, result.Data!.LeftoverBudget);  
        }

        // Негативні тести для фінансів
        [Fact]
        public async Task UpdateTransactionAsync_WhenAmountIsInvalid_ShouldReturnFailure()
        {
            var result = await _service.UpdateTransactionAsync(1, 1, -100, "Помилка");

            Assert.False(result.IsSuccess);
            Assert.Equal("Сума оплати має бути більшою за нуль.", result.ErrorMessage);
            _mockRepo.Verify(r => r.UpdateTransactionAsync(It.IsAny<ExpenseTransaction>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task UpdateTransactionAsync_WhenTransactionNotFound_ShouldReturnFailure()
        {
            int transactionId = 999;
            _mockRepo.Setup(r => r.GetTransactionByIdAsync(transactionId, It.IsAny<CancellationToken>())).ReturnsAsync((ExpenseTransaction?)null);

            var result = await _service.UpdateTransactionAsync(transactionId, 1, 500);
            Assert.False(result.IsSuccess);
            Assert.Equal("Транзакцію не знайдено.", result.ErrorMessage);
        }

        [Fact]
        public async Task DeleteTransactionAsync_WhenTransactionNotFound_ShouldReturnFailure()
        {
            int transactionId = 999;
            _mockRepo.Setup(r => r.DeleteTransactionAsync(transactionId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

            var result = await _service.DeleteTransactionAsync(transactionId);

            Assert.False(result.IsSuccess);
            Assert.Equal("Транзакцію не знайдено для видалення.", result.ErrorMessage);
        }

        [Fact]
        public async Task GetEventFinancialBalanceAsync_WhenEventNotFound_ShouldReturnFailure()
        {
            int eventId = 999;
            _mockRepo.Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>())).ReturnsAsync((Event?)null);

            var result = await _service.GetEventFinancialBalanceAsync(eventId);

            Assert.False(result.IsSuccess);
            Assert.Equal("Подію не знайдено.", result.ErrorMessage);
        }

        // =========================================================================
        // ЮЗ-КЕЙС 12: БАЗА ПІДРЯДНИКІВ ТА ЇХ ПРИЗНАЧЕННЯ (Vendor Management)
        // =========================================================================

        [Fact]
        public async Task GetRecommendedVendorsAsync_ShouldReturnVendors()
        {
            int eventId = 1;
            int planItemId = 10;
            
            var eventMock = new Event { Id = eventId, Budget = 50000m, Style = "Сучасний" };
            var planItemsMock = new List<PlanItem>
            {
                new PlanItem { Id = planItemId, EventId = eventId, Category = "Локація", Cost = 10000m }
            };
            var allVendors = new List<Vendor>
            {
                new Vendor { Id = 1, Name = "Ресторан Веранда", Category = "Локація", BasePrice = 5000m }
            };

            _mockRepo.Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>())).ReturnsAsync(eventMock);
            _mockRepo.Setup(r => r.GetPlanItemsByEventIdAsync(eventId, It.IsAny<CancellationToken>())).ReturnsAsync(planItemsMock);
            _mockRepo.Setup(r => r.GetAllVendorsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(allVendors);

            var result = await _service.GetRecommendedVendorsAsync(eventId, planItemId);

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Single(result.Data!);
        }

        [Fact]
        public async Task AssignVendorToPlanItemAsync_WhenValid_ShouldSucceed()
        {
            int planItemId = 10;
            int vendorId = 1;

            var result = await _service.AssignVendorToPlanItemAsync(planItemId, vendorId);

            Assert.True(result.IsSuccess);
        }

        // Негативні тести для підрядників
        [Fact]
        public async Task GetRecommendedVendorsAsync_WhenEventOrPlanItemNotFound_ShouldReturnFailure()
        {
            int eventId = 1;
            int planItemId = 99;

            _mockRepo.Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>())).ReturnsAsync(new Event { Id = eventId });
            _mockRepo.Setup(r => r.GetPlanItemsByEventIdAsync(eventId, It.IsAny<CancellationToken>())).ReturnsAsync(new List<PlanItem>());

            var result = await _service.GetRecommendedVendorsAsync(eventId, planItemId);

            Assert.False(result.IsSuccess);
            Assert.Equal("Подію або завдання не знайдено.", result.ErrorMessage);
        }

        // =========================================================================
        // ЮЗ-КЕЙС 13: КОНТРОЛЬ БЮДЖЕТУ ТА ПОПЕРЕДЖЕННЯ ПРО ПЕРЕВИЩЕННЯ (Розширено)
        // =========================================================================

        [Fact]
        public async Task GetEventFinancialBalanceAsync_WhenActuallyPaidExceedsBudget_ShouldReflectExceedanceData()
        {
            int eventId = 1;
            var ev = new Event { Id = eventId, Budget = 30000m }; 
            var planItems = new List<PlanItem> { new PlanItem { Id = 1, Cost = 30000m } }; 
            var transactions = new List<ExpenseTransaction> { new ExpenseTransaction { PlanItemId = 1, Amount = 33000m } }; 

            _mockRepo.Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>())).ReturnsAsync(ev);
            _mockRepo.Setup(r => r.GetPlanItemsByEventIdAsync(eventId, It.IsAny<CancellationToken>())).ReturnsAsync(planItems);
            _mockRepo.Setup(r => r.GetTransactionsByEventIdAsync(eventId, It.IsAny<CancellationToken>())).ReturnsAsync(transactions);

            var result = await _service.GetEventFinancialBalanceAsync(eventId);

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            Assert.Equal(30000m, result.Data!.TotalBudget);
            Assert.Equal(33000m, result.Data!.ActuallyPaid);
            
            bool isExceeded = result.Data!.ActuallyPaid > result.Data!.TotalBudget;
            Assert.True(isExceeded);
            Assert.Equal(3000m, result.Data!.ActuallyPaid - result.Data!.TotalBudget);
        }

        [Fact]
        public async Task GetEventFinancialBalanceAsync_WhenPaidWithinBudget_ShouldNotIndicateExceedance()
        {
            int eventId = 2;
            var ev = new Event { Id = eventId, Budget = 30000m }; 
            var planItems = new List<PlanItem> { new PlanItem { Id = 1, Cost = 30000m } }; 
            var transactions = new List<ExpenseTransaction> { new ExpenseTransaction { PlanItemId = 1, Amount = 20000m } }; 

            _mockRepo.Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>())).ReturnsAsync(ev);
            _mockRepo.Setup(r => r.GetPlanItemsByEventIdAsync(eventId, It.IsAny<CancellationToken>())).ReturnsAsync(planItems);
            _mockRepo.Setup(r => r.GetTransactionsByEventIdAsync(eventId, It.IsAny<CancellationToken>())).ReturnsAsync(transactions);

            var result = await _service.GetEventFinancialBalanceAsync(eventId);

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Data);
            
            bool isExceeded = result.Data!.ActuallyPaid > result.Data!.TotalBudget;
            Assert.False(isExceeded);
        }

        [Fact]
        public async Task GetEventFinancialBalanceAsync_WhenZeroBudgetAndZeroExpenses_ShouldHandleGracefully()
        {
            int eventId = 3;
            var ev = new Event { Id = eventId, Budget = 0m }; 
            _mockRepo.Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>())).ReturnsAsync(ev);
            _mockRepo.Setup(r => r.GetPlanItemsByEventIdAsync(eventId, It.IsAny<CancellationToken>())).ReturnsAsync(new List<PlanItem>());
            _mockRepo.Setup(r => r.GetTransactionsByEventIdAsync(eventId, It.IsAny<CancellationToken>())).ReturnsAsync(new List<ExpenseTransaction>());

            var result = await _service.GetEventFinancialBalanceAsync(eventId);

            Assert.True(result.IsSuccess);
            Assert.Equal(0m, result.Data!.TotalBudget);
            Assert.Equal(0m, result.Data!.ActuallyPaid);
            Assert.Equal(0m, result.Data!.LeftoverBudget);
        }

        [Fact]
        public async Task GetEventFinancialBalanceAsync_ForBudgetCheck_WhenEventNotFound_ShouldReturnFailure()
        {
            int nonExistentEventId = 999;
            _mockRepo.Setup(r => r.GetByIdAsync(nonExistentEventId, It.IsAny<CancellationToken>())).ReturnsAsync((Event?)null);

            var result = await _service.GetEventFinancialBalanceAsync(nonExistentEventId);

            Assert.False(result.IsSuccess);
            Assert.Equal("Подію не знайдено.", result.ErrorMessage);
        }

        // =========================================================================
        // ЮЗ-КЕЙС 14: ГЕНЕРАЦІЯ ТА ФОРМУВАННЯ ЗАПРОШЕНЬ (Розширено)
        // =========================================================================

        [Fact]
        public async Task GetOrCreateModelInvitationAsync_WhenInvitationExists_ShouldReturnIt()
        {
            int eventId = 1;
            var existingInvitation = new Invitation { Id = 1, EventId = eventId, CustomText = "Важливе запрошення" };
            
            _mockRepo.Setup(r => r.GetInvitationByEventIdAsync(eventId, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(existingInvitation);

            var result = await _service.GetOrCreateModelInvitationAsync(eventId);

            Assert.True(result.IsSuccess);
            Assert.Equal("Важливе запрошення", result.Data!.CustomText);
            _mockRepo.Verify(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task GetOrCreateModelInvitationAsync_WhenEventNotFound_ShouldReturnFailure()
        {
            int eventId = 99;
            _mockRepo.Setup(r => r.GetInvitationByEventIdAsync(eventId, It.IsAny<CancellationToken>()))
                     .ReturnsAsync((Invitation?)null);
            _mockRepo.Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
                     .ReturnsAsync((Event?)null);

            var result = await _service.GetOrCreateModelInvitationAsync(eventId);

            Assert.False(result.IsSuccess);
            Assert.Equal("Подію не знайдено.", result.ErrorMessage);
        }

        [Fact]
        public async Task SaveInvitationChangesAsync_WhenInvitationIsNull_ShouldReturnFailure()
        {
            var result = await _service.SaveInvitationChangesAsync(null!);

            Assert.False(result.IsSuccess);
            Assert.Equal("Дані запрошення відсутні.", result.ErrorMessage);
        }

        [Fact]
        public async Task SaveInvitationChangesAsync_WhenTextIsEmpty_ShouldReturnFailure()
        {
            var invalidInvitation = new Invitation { Id = 1, CustomText = "   " };

            var result = await _service.SaveInvitationChangesAsync(invalidInvitation);

            Assert.False(result.IsSuccess);
            Assert.Equal("Текст запрошення не може бути порожнім.", result.ErrorMessage);
            _mockRepo.Verify(r => r.SaveInvitationAsync(It.IsAny<Invitation>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        // =========================================================================
        // ЮЗ-КЕЙС 15: КЕРУВАННЯ ПРИМІТКАМИ ГОСТЯ (Guest Notes Management - Розширено)
        // =========================================================================

        [Fact]
        public async Task UpdateGuestAsync_WhenGuestDataIsValid_ShouldUpdateInDatabase()
        {
            var guestToUpdate = new Guest { Id = 5, Name = "Ірина", Notes = "Алергія на горіхи", Status = "Confirmed" };
            
            _mockRepo.Setup(r => r.UpdateGuestAsync(guestToUpdate, It.IsAny<CancellationToken>()))
                     .Returns(Task.CompletedTask);

            var result = await _service.UpdateGuestAsync(guestToUpdate);

            Assert.True(result.IsSuccess);
            _mockRepo.Verify(r => r.UpdateGuestAsync(guestToUpdate, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateGuestAsync_WhenGuestIsNull_ShouldReturnFailure()
        {
            Guest? guestToUpdate = null;

            var result = await _service.UpdateGuestAsync(guestToUpdate!);

            Assert.False(result.IsSuccess);
            Assert.Equal("Дані гостя відсутні.", result.ErrorMessage);
            _mockRepo.Verify(r => r.UpdateGuestAsync(It.IsAny<Guest>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task UpdateGuestAsync_WhenClearingNotes_ShouldSucceed()
        {
            var guestWithClearedNotes = new Guest { Id = 5, Name = "Ірина", Notes = "", Status = "Confirmed" };
            
            _mockRepo.Setup(r => r.UpdateGuestAsync(guestWithClearedNotes, It.IsAny<CancellationToken>()))
                     .Returns(Task.CompletedTask);

            var result = await _service.UpdateGuestAsync(guestWithClearedNotes);

            Assert.True(result.IsSuccess);
            Assert.Equal(string.Empty, guestWithClearedNotes.Notes);
            _mockRepo.Verify(r => r.UpdateGuestAsync(guestWithClearedNotes, It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}