using System.Windows;
using EventStore.BLL.Services;
using EventStore.DAL.Models;

namespace EventStore.WPF;

/// <summary>
/// Represents the main application window.
/// </summary>
public partial class MainWindow : Window
{
    private readonly EventService eventService;
    private readonly Func<Event?, CreateEventWindow> createEventWindowFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow"/> class.
    /// </summary>
    /// <param name="eventService">The event service.</param>
    /// <param name="createEventWindowFactory">Factory to create the event window.</param>
    public MainWindow(
        EventService eventService,
        Func<Event?, CreateEventWindow> createEventWindowFactory)
    {
        this.InitializeComponent();
        this.eventService = eventService ?? throw new ArgumentNullException(nameof(eventService));
        this.createEventWindowFactory = createEventWindowFactory ?? throw new ArgumentNullException(nameof(createEventWindowFactory));

        this.Loaded += this.MainWindow_Loaded;
    }

    /// <summary>
    /// Handles the window load event to fetch events from the database.
    /// </summary>
    /// <param name="sender">The sender of the event.</param>
    /// <param name="e">The event arguments.</param>
    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await this.LoadEventsAsync();
    }

    /// <summary>
    /// Loads all events from the database and displays them.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private async Task LoadEventsAsync()
    {
        try
        {
            var events = await this.eventService.GetAllEventsAsync();
            this.EventsDataGrid.ItemsSource = events;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Помилка завантаження подій: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Opens the event creation window.
    /// </summary>
    /// <param name="sender">The control that raised the event.</param>
    /// <param name="e">The event arguments.</param>
    private async void CreateEvent_Click(object sender, RoutedEventArgs e)
    {
        var window = this.createEventWindowFactory(null);
        if (window.ShowDialog() == true)
        {
            await this.LoadEventsAsync();
        }
    }

    /// <summary>
    /// Opens the event editing window for the selected event.
    /// </summary>
    /// <param name="sender">The control that raised the event.</param>
    /// <param name="e">The event arguments.</param>
    private async void EditEvent_Click(object sender, RoutedEventArgs e)
    {
        if (this.EventsDataGrid.SelectedItem is not Event selectedEvent)
        {
            MessageBox.Show("Будь ласка, оберіть подію для редагування.", "Увага", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var window = this.createEventWindowFactory(selectedEvent);
        if (window.ShowDialog() == true)
        {
            await this.LoadEventsAsync();
        }
    }
}