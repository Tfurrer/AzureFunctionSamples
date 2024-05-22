using Bogus;
using Workflows.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Workflows.Services
{
    public interface IDataContext
    {
        public Prospect GetProspect(string Id);
        public Task UpdateProspectStatus(string Id, string status);
        public List<Prospect> GetProspects();
    }
    public class MockContext: IDataContext
    {
        private Faker<Prospect> faker = new Faker<Prospect>()
            .RuleFor(x => x.Id, x => x.Random.Guid().ToString())
            .RuleFor(x => x.Name, x => x.Name.FullName())
            .RuleFor(x => x.IsExperienced, x => x.Random.Bool(0.25f));
        private List<Prospect> prospects = new List<Prospect>();
        public MockContext()
        {
             prospects = faker.Generate(100)?.ToList();
        }

        public Prospect GetProspect(string Id)
        {
            return prospects.FirstOrDefault(x=> x.Id == Id);
        }

        public List<Prospect> GetProspects()
        {
            return prospects;
        }

        public async Task UpdateProspectStatus(string Id, string status)
        {
            var prospect = prospects.FirstOrDefault(x => x.Id == Id);
            prospect.CurrentStatus = status;
        }
    }
}
