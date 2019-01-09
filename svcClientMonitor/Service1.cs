using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

public partial class Service1 : ServiceBase
{
    //The main timer
    private System.Timers.Timer m_mainTimer;
    //How often to run the routine in milliseconds (minutes * 60000)
    private int scanInterval = 120 * 60000;

    //MongoDB Info
    const string Server = "mongodb://192.168.50.225";
    const string Database = "Monitoring";
    const string Collection = "Machines";

    public Service1()
    {
        InitializeComponent();
    }

    protected override void OnStart(string[] args)
    {
        //Create the Main timer
        m_mainTimer = new System.Timers.Timer
        {
            //Set the timer interval
            Interval = scanInterval
        };
        //Dictate what to do when the event fires
        m_mainTimer.Elapsed += m_mainTimer_Elapsed;
        //Something to do with something, I forgot since it's been a while
        m_mainTimer.AutoReset = true;

#if DEBUG
#else
            m_mainTimer.Start(); //Start timer only in Release
#endif

        //Run 1st Tick Manually
        Routine();
    }

    static void m_mainTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
    {
        //Each interval run the UpdateUsers() function
        Routine();
    }

    public void OnDebug()
    {
        //Manually kick off the service when debugging
        OnStart(null);
    }

    protected override void OnStop()
    {
    }

    private static void Routine()
    {
        Console.Beep();

        var machine = new Machine();

        //Get all of the system info
        using (var windowsMachineInfoSearcher = new ManagementObjectSearcher(@"root\cimv2", "select * from Win32_ComputerSystem"))
        {
            var machineinfos = windowsMachineInfoSearcher.Get();

            foreach (var info in machineinfos)
            {
                try
                {
                    machine.Name = (string)info.Properties["Name"].Value;
                    machine.Manufacturer = (string)info.Properties["Manufacturer"].Value;
                    machine.Model = (string)info.Properties["Model"].Value;
                    machine.Processors = (uint)info.Properties["NumberOfProcessors"].Value;
                    machine.Cores = (uint)info.Properties["NumberOfLogicalProcessors"].Value;
                    machine.Memory = (UInt64)info.Properties["TotalPhysicalMemory"].Value;
                    machine.LastUser = (string)info.Properties["UserName"].Value;
                }
                catch (Exception e) { Console.WriteLine(e); }
                //TODO: Write the data somewhere
            }
        }

        //Get the S/T of the system
        using (var windowsMachineInfoSearcher = new ManagementObjectSearcher(@"root\cimv2", "select * from Win32_SystemEnclosure"))
        {
            var enclosureInfos= windowsMachineInfoSearcher.Get();

            foreach (var info in enclosureInfos)
            {
                try
                {
                    machine.ServiceTag = (string)info.Properties["SerialNumber"].Value;
                }
                catch (Exception e) { Console.WriteLine(e); }
            }
        }

        //Get all of the OS info
        using (var windowsOSinfoSearcher = new ManagementObjectSearcher(@"root\cimv2", "select * from Win32_OperatingSystem"))
        {
            var osinfos = windowsOSinfoSearcher.Get();

            foreach (var info in osinfos)
            {
                try
                {
                    machine.OperatingSystemName = (string)info.Properties["Caption"].Value;
                    machine.OperatingSystemVersion = (string)info.Properties["Version"].Value;
                    machine.OperatingSystemInstalled = (string)info.Properties["InstallDate"].Value;
                    machine.OperatingSystemDevice = (string)info.Properties["SystemDevice"].Value;
                    machine.OperatingSystemDirectory = (string)info.Properties["SystemDirectory"].Value;
                    machine.OperatingSystemSPMajor = (ushort)info.Properties["ServicePackMajorVersion"].Value;
                    machine.OperatingSystemSPMinor = (ushort)info.Properties["ServicePackMinorVersion"].Value;
                }
                catch (Exception e) { Console.WriteLine(e); }
                //TODO: Write the data somewhere
            }
        }

        //Get all of the services on the machine
        using (var windowsServicesSearcher = new ManagementObjectSearcher(@"root\cimv2", "select * from Win32_Service"))
        {
            var serviceList = windowsServicesSearcher.Get();

            foreach (var service in serviceList)
            {
                try
                {
                    var svc = new Service
                    {
                        Name = (string)service.Properties["Name"].Value,
                        Path = (string)service.Properties["PathName"].Value,
                        StartMode = (string)service.Properties["StartMode"].Value,
                        State = (string)service.Properties["State"].Value,
                        Description = (string)service.Properties["Caption"].Value,
                    };

                    machine.Services.Add(svc);
                }
                catch (Exception e) { Console.WriteLine(e); }
            }
        }

        //Get all of the applications on the machine
        using (var windowsProgramsSearcher = new ManagementObjectSearcher(@"root\cimv2", "select * from Win32_Product"))
        {
            var programList = windowsProgramsSearcher.Get();

            foreach (var program in programList)
            {
                try
                {
                    var app = new Application
                    {
                        Id = (string)program.Properties["IdentifyingNumber"].Value,
                        Name = (string)program.Properties["Name"].Value,
                        Installed = (string)program.Properties["InstallDate"].Value,
                        Package = (string)program.Properties["PackageName"].Value,
                        Vendor = (string)program.Properties["Vendor"].Value,
                        Version = (string)program.Properties["Version"].Value,
                        Description = (string)program.Properties["Caption"].Value
                    };

                    machine.Applications.Add(app);
                }
                catch(Exception e) { Console.WriteLine(e); }
            }
        }

        //Get all of the network info
        var adapters = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces();

        foreach (var adapter in adapters)
        {
            try
            {
                var network = new Network
                {
                    Name = adapter.Name,
                    MACAddress = adapter.GetPhysicalAddress().ToString(),
                    IPAddress = adapter.GetIPProperties().UnicastAddresses[0].Address.ToString(),
                    IPNetMask = adapter.GetIPProperties().UnicastAddresses[0].IPv4Mask.ToString()
                };

                if(network.MACAddress != "")
                    machine.Networks.Add(network);
            }
            catch (Exception e) { Console.WriteLine(e); }
        };

        WriteMachineToDB(machine);

        Console.Beep(1000,1000);
    }

    private static void WriteMachineToDB(Machine machine)
    {
        try
        {
            var collection = new MongoClient(Server)
                .GetDatabase(Database)
                .GetCollection<Machine>(Collection);

            var existing = collection.AsQueryable()
                .FirstOrDefault(m => m.Name == machine.Name);

            if (existing == null)
            {
                collection.InsertOne(machine);
            }
            else
            {
                var filter = Builders<Machine>.Filter.Eq(m => m.Name, existing.Name);
                machine.Id = existing.Id;

                collection.ReplaceOne(filter, machine);
            }
        }
        catch (Exception e) { Console.WriteLine(e); }
    }
}
