using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class Machine
{
    public MongoDB.Bson.ObjectId Id { get; set; }
    public string Name { get; set; }
    
    public string Manufacturer { get; set; }
    public string Model { get; set; }
    public string ServiceTag { get; set; }
    public UInt64 Processors { get; set; }
    public UInt64 Cores { get; set; }
    public UInt64 Memory { get; set; }

    public string OperatingSystemName { get; set; }
    public string OperatingSystemVersion { get; set; }
    public string OperatingSystemDevice { get; set; }
    public string OperatingSystemDirectory { get; set; }
    public string OperatingSystemInstalled { get; set; }
    public UInt64 OperatingSystemSPMajor { get; set; }
    public UInt64 OperatingSystemSPMinor { get; set; }

    public string LastUser { get; set; }

    public List<Application> Applications { get; set; }
    public List<Service> Services { get; set; }
    public List<Network> Networks { get; set; }

    public DateTime Updated { get; set; }

    public Machine()
    {
        Services = new List<Service>();
        Applications = new List<Application>();
        Networks = new List<Network>();
        Updated = DateTime.Now;
    }
}
