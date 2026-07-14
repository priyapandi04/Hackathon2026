-- ============================================================
-- UPS ReLoop Nexus - Realistic Test Data INSERT Script
-- Generated for: Hyperlocal Inventory Exchange Network (HIEN)
-- Tables: Packages, ReturnRequests, ImageValidationResults,
--         InventoryPool, DemandHistory, AgentRecommendations
-- ============================================================
-- NOTE: Your schema has no [MatchAgentResults] table.
--       Match data is captured in InventoryPool + AgentRecommendations.
-- ============================================================

SET NOCOUNT ON;
GO

-- ============================================================
-- SECTION 1: Packages (50 records)
-- ============================================================
DECLARE @Now DATETIME2 = SYSUTCDATETIME();

INSERT INTO [dbo].[Packages]
	([Id], [TrackingNumber], [SenderName], [SenderAddress], [RecipientName], [RecipientAddress], [Weight], [Status], [AiRecommendation], [IsReturnable], [ReturnInitiatedAt], [CreatedAt], [IsDeleted])
VALUES
-- Chennai
('10000000-0001-0001-0001-000000000001', '1Z999AA10001', 'Nike Store Chennai', '45 Anna Salai, Chennai', 'Rahul Sharma', '12 Adyar Main Rd, Chennai', 1.20, 'Delivered', NULL, 1, DATEADD(DAY,-88,@Now), DATEADD(DAY,-90,@Now), 0),
('10000000-0001-0001-0001-000000000002', '1Z999AA10002', 'Adidas Outlet Chennai', '78 Mount Rd, Chennai', 'Priya Krishnan', '5 Velachery, Chennai', 1.80, 'Delivered', NULL, 1, DATEADD(DAY,-85,@Now), DATEADD(DAY,-87,@Now), 0),
('10000000-0001-0001-0001-000000000003', '1Z999AA10003', 'Apple Authorized Chennai', '22 T Nagar, Chennai', 'Karthik Rajan', '90 Porur, Chennai', 0.25, 'Delivered', NULL, 1, DATEADD(DAY,-82,@Now), DATEADD(DAY,-84,@Now), 0),
('10000000-0001-0001-0001-000000000004', '1Z999AA10004', 'Samsung Store Chennai', '15 Nungambakkam, Chennai', 'Deepa Venkat', '33 Tambaram, Chennai', 0.20, 'Delivered', NULL, 1, DATEADD(DAY,-80,@Now), DATEADD(DAY,-82,@Now), 0),
('10000000-0001-0001-0001-000000000005', '1Z999AA10005', 'Puma Outlet Chennai', '8 Express Ave, Chennai', 'Arun Kumar', '67 Chrompet, Chennai', 0.40, 'Delivered', NULL, 1, DATEADD(DAY,-78,@Now), DATEADD(DAY,-80,@Now), 0),
('10000000-0001-0001-0001-000000000006', '1Z999AA10006', 'Levis Store Chennai', '3 Phoenix Mall, Chennai', 'Meena Iyer', '21 Guindy, Chennai', 0.90, 'Delivered', NULL, 1, DATEADD(DAY,-75,@Now), DATEADD(DAY,-77,@Now), 0),
('10000000-0001-0001-0001-000000000007', '1Z999AA10007', 'Dell India Chennai', '55 Tidel Park, Chennai', 'Vijay Anand', '44 Sholinganallur, Chennai', 0.15, 'Delivered', NULL, 1, DATEADD(DAY,-73,@Now), DATEADD(DAY,-75,@Now), 0),
('10000000-0001-0001-0001-000000000008', '1Z999AA10008', 'Logitech India', '10 OMR, Chennai', 'Sathish Babu', '88 Medavakkam, Chennai', 0.80, 'Delivered', NULL, 1, DATEADD(DAY,-70,@Now), DATEADD(DAY,-72,@Now), 0),
-- Bangalore
('10000000-0001-0001-0001-000000000009', '1Z999AA10009', 'Nike Store Bangalore', '12 MG Road, Bangalore', 'Anitha Rao', '56 Whitefield, Bangalore', 1.10, 'Delivered', NULL, 1, DATEADD(DAY,-68,@Now), DATEADD(DAY,-70,@Now), 0),
('10000000-0001-0001-0001-000000000010', '1Z999AA10010', 'Adidas Bangalore', '34 Brigade Rd, Bangalore', 'Suresh Gowda', '78 Electronic City, Bangalore', 1.90, 'Delivered', NULL, 1, DATEADD(DAY,-66,@Now), DATEADD(DAY,-68,@Now), 0),
('10000000-0001-0001-0001-000000000011', '1Z999AA10011', 'Apple Store Bangalore', '5 Indiranagar, Bangalore', 'Lakshmi Devi', '23 Koramangala, Bangalore', 0.22, 'Delivered', NULL, 1, DATEADD(DAY,-64,@Now), DATEADD(DAY,-66,@Now), 0),
('10000000-0001-0001-0001-000000000012', '1Z999AA10012', 'Samsung Bangalore', '67 Jayanagar, Bangalore', 'Mohan Das', '11 HSR Layout, Bangalore', 0.18, 'Delivered', NULL, 1, DATEADD(DAY,-62,@Now), DATEADD(DAY,-64,@Now), 0),
('10000000-0001-0001-0001-000000000013', '1Z999AA10013', 'Puma Store Bangalore', '89 UB City, Bangalore', 'Kavitha Murthy', '45 Marathahalli, Bangalore', 0.35, 'Delivered', NULL, 1, DATEADD(DAY,-60,@Now), DATEADD(DAY,-62,@Now), 0),
('10000000-0001-0001-0001-000000000014', '1Z999AA10014', 'Levis Bangalore', '2 Forum Mall, Bangalore', 'Ravi Shankar', '90 Yelahanka, Bangalore', 0.85, 'Delivered', NULL, 1, DATEADD(DAY,-58,@Now), DATEADD(DAY,-60,@Now), 0),
('10000000-0001-0001-0001-000000000015', '1Z999AA10015', 'HP Store Bangalore', '14 Manyata Park, Bangalore', 'Nandini Raj', '37 Hebbal, Bangalore', 0.70, 'Delivered', NULL, 1, DATEADD(DAY,-56,@Now), DATEADD(DAY,-58,@Now), 0),
('10000000-0001-0001-0001-000000000016', '1Z999AA10016', 'Amazon Hub Bangalore', '6 Bellandur, Bangalore', 'Prasad Hegde', '52 Sarjapur, Bangalore', 1.50, 'Delivered', NULL, 1, DATEADD(DAY,-54,@Now), DATEADD(DAY,-56,@Now), 0),
-- Hyderabad
('10000000-0001-0001-0001-000000000017', '1Z999AA10017', 'Nike Hyderabad', '30 Banjara Hills, Hyderabad', 'Srikanth Reddy', '15 Gachibowli, Hyderabad', 1.25, 'Delivered', NULL, 1, DATEADD(DAY,-52,@Now), DATEADD(DAY,-54,@Now), 0),
('10000000-0001-0001-0001-000000000018', '1Z999AA10018', 'Adidas Hyderabad', '42 Jubilee Hills, Hyderabad', 'Padma Lakshmi', '28 Madhapur, Hyderabad', 1.75, 'Delivered', NULL, 1, DATEADD(DAY,-50,@Now), DATEADD(DAY,-52,@Now), 0),
('10000000-0001-0001-0001-000000000019', '1Z999AA10019', 'Apple Hyderabad', '18 Hitech City, Hyderabad', 'Venkat Rao', '63 Kondapur, Hyderabad', 0.24, 'Delivered', NULL, 1, DATEADD(DAY,-48,@Now), DATEADD(DAY,-50,@Now), 0),
('10000000-0001-0001-0001-000000000020', '1Z999AA10020', 'Samsung Hyderabad', '55 Kukatpally, Hyderabad', 'Anusha Reddy', '71 Miyapur, Hyderabad', 0.19, 'Delivered', NULL, 1, DATEADD(DAY,-46,@Now), DATEADD(DAY,-48,@Now), 0),
('10000000-0001-0001-0001-000000000021', '1Z999AA10021', 'Dell Store Hyderabad', '9 HITEC City, Hyderabad', 'Rajesh Gupta', '40 Begumpet, Hyderabad', 0.14, 'Delivered', NULL, 1, DATEADD(DAY,-44,@Now), DATEADD(DAY,-46,@Now), 0),
('10000000-0001-0001-0001-000000000022', '1Z999AA10022', 'Logitech Hyderabad', '25 Ameerpet, Hyderabad', 'Swathi Naidu', '82 Secunderabad, Hyderabad', 0.78, 'Delivered', NULL, 1, DATEADD(DAY,-42,@Now), DATEADD(DAY,-44,@Now), 0),
('10000000-0001-0001-0001-000000000023', '1Z999AA10023', 'HP Store Hyderabad', '37 Somajiguda, Hyderabad', 'Manoj Kumar', '19 LB Nagar, Hyderabad', 0.65, 'Delivered', NULL, 1, DATEADD(DAY,-40,@Now), DATEADD(DAY,-42,@Now), 0),
('10000000-0001-0001-0001-000000000024', '1Z999AA10024', 'Amazon Hyderabad', '48 Financial Dist, Hyderabad', 'Divya Teja', '55 Uppal, Hyderabad', 1.45, 'Delivered', NULL, 1, DATEADD(DAY,-38,@Now), DATEADD(DAY,-40,@Now), 0),
-- Mumbai
('10000000-0001-0001-0001-000000000025', '1Z999AA10025', 'Nike Store Mumbai', '10 Linking Rd, Mumbai', 'Amit Patel', '32 Andheri West, Mumbai', 1.30, 'Delivered', NULL, 1, DATEADD(DAY,-36,@Now), DATEADD(DAY,-38,@Now), 0),
('10000000-0001-0001-0001-000000000026', '1Z999AA10026', 'Adidas Mumbai', '22 Bandra, Mumbai', 'Sneha Joshi', '65 Powai, Mumbai', 1.85, 'Delivered', NULL, 1, DATEADD(DAY,-34,@Now), DATEADD(DAY,-36,@Now), 0),
('10000000-0001-0001-0001-000000000027', '1Z999AA10027', 'Apple Mumbai', '7 Colaba, Mumbai', 'Rohan Mehta', '41 Worli, Mumbai', 0.23, 'Delivered', NULL, 1, DATEADD(DAY,-32,@Now), DATEADD(DAY,-34,@Now), 0),
('10000000-0001-0001-0001-000000000028', '1Z999AA10028', 'Samsung Mumbai', '33 Lower Parel, Mumbai', 'Pooja Shah', '77 Thane, Mumbai', 0.21, 'Delivered', NULL, 1, DATEADD(DAY,-30,@Now), DATEADD(DAY,-32,@Now), 0),
('10000000-0001-0001-0001-000000000029', '1Z999AA10029', 'Puma Mumbai', '50 Phoenix Palladium, Mumbai', 'Nikhil Desai', '14 Borivali, Mumbai', 0.38, 'Delivered', NULL, 1, DATEADD(DAY,-28,@Now), DATEADD(DAY,-30,@Now), 0),
('10000000-0001-0001-0001-000000000030', '1Z999AA10030', 'Levis Mumbai', '16 High Street Phoenix, Mumbai', 'Tanvi Kapoor', '89 Malad, Mumbai', 0.92, 'Delivered', NULL, 1, DATEADD(DAY,-26,@Now), DATEADD(DAY,-28,@Now), 0),
('10000000-0001-0001-0001-000000000031', '1Z999AA10031', 'Dell Mumbai', '28 BKC, Mumbai', 'Harsh Vardhan', '53 Goregaon, Mumbai', 0.16, 'Delivered', NULL, 1, DATEADD(DAY,-24,@Now), DATEADD(DAY,-26,@Now), 0),
('10000000-0001-0001-0001-000000000032', '1Z999AA10032', 'Amazon Mumbai', '60 Navi Mumbai', 'Ritika Singhania', '26 Vashi, Mumbai', 1.55, 'Delivered', NULL, 1, DATEADD(DAY,-22,@Now), DATEADD(DAY,-24,@Now), 0),
-- Delhi
('10000000-0001-0001-0001-000000000033', '1Z999AA10033', 'Nike Store Delhi', '5 Connaught Place, Delhi', 'Arjun Singh', '38 Dwarka, Delhi', 1.15, 'Delivered', NULL, 1, DATEADD(DAY,-20,@Now), DATEADD(DAY,-22,@Now), 0),
('10000000-0001-0001-0001-000000000034', '1Z999AA10034', 'Adidas Delhi', '18 South Ex, Delhi', 'Neha Gupta', '72 Rohini, Delhi', 1.70, 'Delivered', NULL, 1, DATEADD(DAY,-18,@Now), DATEADD(DAY,-20,@Now), 0),
('10000000-0001-0001-0001-000000000035', '1Z999AA10035', 'Apple Delhi', '3 Khan Market, Delhi', 'Vikram Malhotra', '47 Vasant Kunj, Delhi', 0.26, 'Delivered', NULL, 1, DATEADD(DAY,-16,@Now), DATEADD(DAY,-18,@Now), 0),
('10000000-0001-0001-0001-000000000036', '1Z999AA10036', 'Samsung Delhi', '41 Lajpat Nagar, Delhi', 'Ishita Verma', '83 Janakpuri, Delhi', 0.17, 'Delivered', NULL, 1, DATEADD(DAY,-14,@Now), DATEADD(DAY,-16,@Now), 0),
('10000000-0001-0001-0001-000000000037', '1Z999AA10037', 'Puma Delhi', '27 Select Citywalk, Delhi', 'Gaurav Khanna', '59 Pitampura, Delhi', 0.42, 'Delivered', NULL, 1, DATEADD(DAY,-12,@Now), DATEADD(DAY,-14,@Now), 0),
('10000000-0001-0001-0001-000000000038', '1Z999AA10038', 'Levis Delhi', '14 DLF Promenade, Delhi', 'Simran Kaur', '31 Saket, Delhi', 0.88, 'Delivered', NULL, 1, DATEADD(DAY,-10,@Now), DATEADD(DAY,-12,@Now), 0),
('10000000-0001-0001-0001-000000000039', '1Z999AA10039', 'Logitech Delhi', '52 Nehru Place, Delhi', 'Manish Tiwari', '66 Noida Sec 62, Delhi', 0.82, 'Delivered', NULL, 1, DATEADD(DAY,-8,@Now), DATEADD(DAY,-10,@Now), 0),
('10000000-0001-0001-0001-000000000040', '1Z999AA10040', 'HP Store Delhi', '8 Cyber Hub, Delhi', 'Anjali Bhatt', '20 Gurgaon, Delhi', 0.68, 'Delivered', NULL, 1, DATEADD(DAY,-6,@Now), DATEADD(DAY,-8,@Now), 0),
-- Pune
('10000000-0001-0001-0001-000000000041', '1Z999AA10041', 'Nike Store Pune', '15 FC Road, Pune', 'Sachin Kulkarni', '42 Kothrud, Pune', 1.22, 'Delivered', NULL, 1, DATEADD(DAY,-5,@Now), DATEADD(DAY,-7,@Now), 0),
('10000000-0001-0001-0001-000000000042', '1Z999AA10042', 'Adidas Pune', '29 MG Road, Pune', 'Rashmi Deshpande', '58 Hinjewadi, Pune', 1.65, 'Delivered', NULL, 1, DATEADD(DAY,-4,@Now), DATEADD(DAY,-6,@Now), 0),
('10000000-0001-0001-0001-000000000043', '1Z999AA10043', 'Apple Pune', '7 Koregaon Park, Pune', 'Tushar Patil', '35 Wakad, Pune', 0.25, 'Delivered', NULL, 1, DATEADD(DAY,-3,@Now), DATEADD(DAY,-5,@Now), 0),
('10000000-0001-0001-0001-000000000044', '1Z999AA10044', 'Samsung Pune', '51 Viman Nagar, Pune', 'Aparna Jog', '73 Baner, Pune', 0.20, 'Delivered', NULL, 1, DATEADD(DAY,-2,@Now), DATEADD(DAY,-4,@Now), 0),
('10000000-0001-0001-0001-000000000045', '1Z999AA10045', 'Puma Pune', '63 Phoenix Market, Pune', 'Omkar Shinde', '16 Aundh, Pune', 0.37, 'Delivered', NULL, 1, DATEADD(DAY,-1,@Now), DATEADD(DAY,-3,@Now), 0),
('10000000-0001-0001-0001-000000000046', '1Z999AA10046', 'Levis Pune', '4 Seasons Mall, Pune', 'Gauri Patwardhan', '84 Hadapsar, Pune', 0.91, 'Delivered', NULL, 1, DATEADD(DAY,-1,@Now), DATEADD(DAY,-3,@Now), 0),
('10000000-0001-0001-0001-000000000047', '1Z999AA10047', 'Dell Pune', '20 Magarpatta, Pune', 'Siddharth Jain', '49 Kharadi, Pune', 0.13, 'Delivered', NULL, 1, DATEADD(DAY,-2,@Now), DATEADD(DAY,-4,@Now), 0),
('10000000-0001-0001-0001-000000000048', '1Z999AA10048', 'Logitech Pune', '36 EON IT Park, Pune', 'Vaishali More', '61 Pimpri, Pune', 0.76, 'Delivered', NULL, 1, DATEADD(DAY,-3,@Now), DATEADD(DAY,-5,@Now), 0),
('10000000-0001-0001-0001-000000000049', '1Z999AA10049', 'HP Store Pune', '44 Shivaji Nagar, Pune', 'Akash Pawar', '27 Chinchwad, Pune', 0.72, 'Delivered', NULL, 1, DATEADD(DAY,-4,@Now), DATEADD(DAY,-6,@Now), 0),
('10000000-0001-0001-0001-000000000050', '1Z999AA10050', 'Amazon Pune', '58 Hinjewadi Phase 1, Pune', 'Megha Bhosale', '39 PCMC, Pune', 1.48, 'Delivered', NULL, 1, DATEADD(DAY,-5,@Now), DATEADD(DAY,-7,@Now), 0);
GO

-- ============================================================
-- SECTION 2: ReturnRequests (50 records linked to Packages)
-- Status: ~70% Eligible/Matched/Diverted, ~30% Rejected/Pending
-- ============================================================
DECLARE @Now DATETIME2 = SYSUTCDATETIME();

INSERT INTO [dbo].[ReturnRequests]
	([Id], [PackageId], [Reason], [Status], [AiAnalysis], [ResolutionNotes], [ResolvedAt], [CreatedAt], [IsDeleted])
VALUES
('20000000-0002-0001-0001-000000000001', '10000000-0001-0001-0001-000000000001', 'Wrong Size', 'Matched', 'AI: Item condition validated. Local match found in Chennai.', 'Diverted to local buyer', DATEADD(DAY,-86,@Now), DATEADD(DAY,-87,@Now), 0),
('20000000-0002-0001-0001-000000000002', '10000000-0001-0001-0001-000000000002', 'Changed Mind', 'Eligible', 'AI: Item in good condition. Awaiting match.', NULL, NULL, DATEADD(DAY,-84,@Now), 0),
('20000000-0002-0001-0001-000000000003', '10000000-0001-0001-0001-000000000003', 'Defective', 'Rejected', 'AI: Item damaged beyond reuse threshold.', 'Return to warehouse', DATEADD(DAY,-80,@Now), DATEADD(DAY,-81,@Now), 0),
('20000000-0002-0001-0001-000000000004', '10000000-0001-0001-0001-000000000004', 'Ordered Wrong Item', 'Matched', 'AI: Like new condition. High demand in Chennai.', 'Matched to local order', DATEADD(DAY,-78,@Now), DATEADD(DAY,-79,@Now), 0),
('20000000-0002-0001-0001-000000000005', '10000000-0001-0001-0001-000000000005', 'Color Mismatch', 'Eligible', 'AI: Fair condition. Eligible for pool.', NULL, NULL, DATEADD(DAY,-77,@Now), 0),
('20000000-0002-0001-0001-000000000006', '10000000-0001-0001-0001-000000000006', 'Wrong Size', 'Matched', 'AI: Good condition. Matched in Chennai region.', 'Diverted locally', DATEADD(DAY,-73,@Now), DATEADD(DAY,-74,@Now), 0),
('20000000-0002-0001-0001-000000000007', '10000000-0001-0001-0001-000000000007', 'Changed Mind', 'Eligible', 'AI: Like new. Added to inventory pool.', NULL, NULL, DATEADD(DAY,-72,@Now), 0),
('20000000-0002-0001-0001-000000000008', '10000000-0001-0001-0001-000000000008', 'Packaging Damaged', 'Rejected', 'AI: Packaging severely damaged. Item compromised.', 'Return to vendor', DATEADD(DAY,-68,@Now), DATEADD(DAY,-69,@Now), 0),
('20000000-0002-0001-0001-000000000009', '10000000-0001-0001-0001-000000000009', 'Wrong Size', 'Matched', 'AI: Good condition. Demand signal strong in Bangalore.', 'Local match confirmed', DATEADD(DAY,-66,@Now), DATEADD(DAY,-67,@Now), 0),
('20000000-0002-0001-0001-000000000010', '10000000-0001-0001-0001-000000000010', 'Defective', 'Eligible', 'AI: Minor defect. Fair condition. Still eligible.', NULL, NULL, DATEADD(DAY,-65,@Now), 0),
('20000000-0002-0001-0001-000000000011', '10000000-0001-0001-0001-000000000011', 'Changed Mind', 'Matched', 'AI: Like new AirPods. High local demand.', 'Matched to Koramangala buyer', DATEADD(DAY,-62,@Now), DATEADD(DAY,-63,@Now), 0),
('20000000-0002-0001-0001-000000000012', '10000000-0001-0001-0001-000000000012', 'Ordered Wrong Item', 'Eligible', 'AI: Good condition. Pool candidate.', NULL, NULL, DATEADD(DAY,-61,@Now), 0),
('20000000-0002-0001-0001-000000000013', '10000000-0001-0001-0001-000000000013', 'Color Mismatch', 'Matched', 'AI: Like new. Strong category demand.', 'Diverted in Bangalore', DATEADD(DAY,-58,@Now), DATEADD(DAY,-59,@Now), 0),
('20000000-0002-0001-0001-000000000014', '10000000-0001-0001-0001-000000000014', 'Wrong Size', 'Eligible', 'AI: Good condition jeans. Eligible.', NULL, NULL, DATEADD(DAY,-57,@Now), 0),
('20000000-0002-0001-0001-000000000015', '10000000-0001-0001-0001-000000000015', 'Packaging Damaged', 'Rejected', 'AI: Backpack damaged. Not eligible.', 'Returned to warehouse', DATEADD(DAY,-54,@Now), DATEADD(DAY,-55,@Now), 0),
('20000000-0002-0001-0001-000000000016', '10000000-0001-0001-0001-000000000016', 'Changed Mind', 'Matched', 'AI: Like new Echo. Excellent local demand.', 'Matched in Bangalore', DATEADD(DAY,-52,@Now), DATEADD(DAY,-53,@Now), 0),
('20000000-0002-0001-0001-000000000017', '10000000-0001-0001-0001-000000000017', 'Wrong Size', 'Matched', 'AI: Good condition jacket. Hyderabad match.', 'Local diversion done', DATEADD(DAY,-50,@Now), DATEADD(DAY,-51,@Now), 0),
('20000000-0002-0001-0001-000000000018', '10000000-0001-0001-0001-000000000018', 'Defective', 'Eligible', 'AI: Fair condition. Minor sole wear.', NULL, NULL, DATEADD(DAY,-49,@Now), 0),
('20000000-0002-0001-0001-000000000019', '10000000-0001-0001-0001-000000000019', 'Ordered Wrong Item', 'Matched', 'AI: Like new AirPods. Immediate match.', 'Matched in Hyderabad', DATEADD(DAY,-46,@Now), DATEADD(DAY,-47,@Now), 0),
('20000000-0002-0001-0001-000000000020', '10000000-0001-0001-0001-000000000020', 'Color Mismatch', 'Rejected', 'AI: Item damaged during return transit.', 'Vendor credit issued', DATEADD(DAY,-44,@Now), DATEADD(DAY,-45,@Now), 0),
('20000000-0002-0001-0001-000000000021', '10000000-0001-0001-0001-000000000021', 'Changed Mind', 'Eligible', 'AI: Like new mouse. Added to pool.', NULL, NULL, DATEADD(DAY,-43,@Now), 0),
('20000000-0002-0001-0001-000000000022', '10000000-0001-0001-0001-000000000022', 'Wrong Size', 'Matched', 'AI: Good keyboard. Matched locally.', 'Diverted in Hyderabad', DATEADD(DAY,-40,@Now), DATEADD(DAY,-41,@Now), 0),
('20000000-0002-0001-0001-000000000023', '10000000-0001-0001-0001-000000000023', 'Packaging Damaged', 'Rejected', 'AI: Damaged backpack. Below threshold.', 'Return to origin', DATEADD(DAY,-38,@Now), DATEADD(DAY,-39,@Now), 0),
('20000000-0002-0001-0001-000000000024', '10000000-0001-0001-0001-000000000024', 'Defective', 'Eligible', 'AI: Fair condition Echo. Eligible.', NULL, NULL, DATEADD(DAY,-37,@Now), 0),
('20000000-0002-0001-0001-000000000025', '10000000-0001-0001-0001-000000000025', 'Wrong Size', 'Matched', 'AI: Like new jacket. Mumbai match found.', 'Local match Mumbai', DATEADD(DAY,-34,@Now), DATEADD(DAY,-35,@Now), 0),
('20000000-0002-0001-0001-000000000026', '10000000-0001-0001-0001-000000000026', 'Changed Mind', 'Eligible', 'AI: Good shoes. In pool.', NULL, NULL, DATEADD(DAY,-33,@Now), 0),
('20000000-0002-0001-0001-000000000027', '10000000-0001-0001-0001-000000000027', 'Ordered Wrong Item', 'Matched', 'AI: Like new AirPods. High demand Mumbai.', 'Matched locally', DATEADD(DAY,-30,@Now), DATEADD(DAY,-31,@Now), 0),
('20000000-0002-0001-0001-000000000028', '10000000-0001-0001-0001-000000000028', 'Defective', 'Rejected', 'AI: Earbuds non-functional. Damaged.', 'Warranty claim filed', DATEADD(DAY,-28,@Now), DATEADD(DAY,-29,@Now), 0),
('20000000-0002-0001-0001-000000000029', '10000000-0001-0001-0001-000000000029', 'Color Mismatch', 'Eligible', 'AI: Good condition T-shirt. Eligible.', NULL, NULL, DATEADD(DAY,-27,@Now), 0),
('20000000-0002-0001-0001-000000000030', '10000000-0001-0001-0001-000000000030', 'Wrong Size', 'Matched', 'AI: Like new jeans. Strong Mumbai demand.', 'Diverted Mumbai', DATEADD(DAY,-24,@Now), DATEADD(DAY,-25,@Now), 0),
('20000000-0002-0001-0001-000000000031', '10000000-0001-0001-0001-000000000031', 'Changed Mind', 'Eligible', 'AI: Like new mouse. Pool candidate.', NULL, NULL, DATEADD(DAY,-23,@Now), 0),
('20000000-0002-0001-0001-000000000032', '10000000-0001-0001-0001-000000000032', 'Packaging Damaged', 'Rejected', 'AI: Echo box crushed. Item may be compromised.', 'Inspection required', DATEADD(DAY,-20,@Now), DATEADD(DAY,-21,@Now), 0),
('20000000-0002-0001-0001-000000000033', '10000000-0001-0001-0001-000000000033', 'Wrong Size', 'Matched', 'AI: Good jacket. Delhi local match.', 'Matched Delhi', DATEADD(DAY,-18,@Now), DATEADD(DAY,-19,@Now), 0),
('20000000-0002-0001-0001-000000000034', '10000000-0001-0001-0001-000000000034', 'Defective', 'Eligible', 'AI: Fair condition shoes. Minor scuff.', NULL, NULL, DATEADD(DAY,-17,@Now), 0),
('20000000-0002-0001-0001-000000000035', '10000000-0001-0001-0001-000000000035', 'Ordered Wrong Item', 'Matched', 'AI: Like new AirPods. Instant Delhi match.', 'Matched in Delhi', DATEADD(DAY,-14,@Now), DATEADD(DAY,-15,@Now), 0),
('20000000-0002-0001-0001-000000000036', '10000000-0001-0001-0001-000000000036', 'Color Mismatch', 'Eligible', 'AI: Good earbuds. Eligible for pool.', NULL, NULL, DATEADD(DAY,-13,@Now), 0),
('20000000-0002-0001-0001-000000000037', '10000000-0001-0001-0001-000000000037', 'Changed Mind', 'Matched', 'AI: Like new T-shirt. Matched Delhi.', 'Local diversion', DATEADD(DAY,-10,@Now), DATEADD(DAY,-11,@Now), 0),
('20000000-0002-0001-0001-000000000038', '10000000-0001-0001-0001-000000000038', 'Wrong Size', 'Eligible', 'AI: Good jeans. Added to pool.', NULL, NULL, DATEADD(DAY,-9,@Now), 0),
('20000000-0002-0001-0001-000000000039', '10000000-0001-0001-0001-000000000039', 'Defective', 'Rejected', 'AI: Keyboard keys broken. Damaged.', 'Return to vendor', DATEADD(DAY,-6,@Now), DATEADD(DAY,-7,@Now), 0),
('20000000-0002-0001-0001-000000000040', '10000000-0001-0001-0001-000000000040', 'Packaging Damaged', 'Rejected', 'AI: Backpack torn. Not eligible.', 'Disposed', DATEADD(DAY,-4,@Now), DATEADD(DAY,-5,@Now), 0),
('20000000-0002-0001-0001-000000000041', '10000000-0001-0001-0001-000000000041', 'Wrong Size', 'Matched', 'AI: Like new jacket. Pune match.', 'Matched Pune', DATEADD(DAY,-3,@Now), DATEADD(DAY,-4,@Now), 0),
('20000000-0002-0001-0001-000000000042', '10000000-0001-0001-0001-000000000042', 'Changed Mind', 'Eligible', 'AI: Good shoes. In pool.', NULL, NULL, DATEADD(DAY,-3,@Now), 0),
('20000000-0002-0001-0001-000000000043', '10000000-0001-0001-0001-000000000043', 'Ordered Wrong Item', 'Matched', 'AI: Like new AirPods. Pune demand high.', 'Matched Pune', DATEADD(DAY,-2,@Now), DATEADD(DAY,-3,@Now), 0),
('20000000-0002-0001-0001-000000000044', '10000000-0001-0001-0001-000000000044', 'Defective', 'Eligible', 'AI: Fair earbuds. Minor issue. Eligible.', NULL, NULL, DATEADD(DAY,-1,@Now), 0),
('20000000-0002-0001-0001-000000000045', '10000000-0001-0001-0001-000000000045', 'Color Mismatch', 'Matched', 'AI: Like new T-shirt. Pune match.', 'Diverted Pune', DATEADD(DAY,-1,@Now), DATEADD(DAY,-2,@Now), 0),
('20000000-0002-0001-0001-000000000046', '10000000-0001-0001-0001-000000000046', 'Wrong Size', 'Eligible', 'AI: Good jeans. Pool eligible.', NULL, NULL, DATEADD(DAY,-1,@Now), 0),
('20000000-0002-0001-0001-000000000047', '10000000-0001-0001-0001-000000000047', 'Changed Mind', 'Matched', 'AI: Like new mouse. Pune match.', 'Local match', DATEADD(DAY,-1,@Now), DATEADD(DAY,-2,@Now), 0),
('20000000-0002-0001-0001-000000000048', '10000000-0001-0001-0001-000000000048', 'Packaging Damaged', 'Rejected', 'AI: Keyboard box crushed. Keys damaged.', 'Return to vendor', DATEADD(DAY,-2,@Now), DATEADD(DAY,-3,@Now), 0),
('20000000-0002-0001-0001-000000000049', '10000000-0001-0001-0001-000000000049', 'Defective', 'Eligible', 'AI: Fair backpack. Strap loose. Eligible.', NULL, NULL, DATEADD(DAY,-3,@Now), 0),
('20000000-0002-0001-0001-000000000050', '10000000-0001-0001-0001-000000000050', 'Wrong Size', 'Diverted', 'AI: Like new Echo. Diverted to nearby hub.', 'Sent to Hinjewadi hub', DATEADD(DAY,-4,@Now), DATEADD(DAY,-5,@Now), 0);
GO

-- ============================================================
-- SECTION 3: ImageValidationResults (50 records)
-- ~70% Eligible (Like New/Good/Fair), ~30% Not Eligible (Damaged)
-- ============================================================
DECLARE @Now DATETIME2 = SYSUTCDATETIME();

INSERT INTO [dbo].[ImageValidationResults]
	([Id], [ProductId], [ProductName], [Category], [ReturnReason], [Condition], [Eligibility], [Confidence], [Location], [ReturnDate], [CreatedAt], [IsDeleted])
VALUES
-- Chennai (8 records: 6 eligible, 2 not)
('30000000-0003-0001-0001-000000000001', 'PROD-NIKE-001', 'Nike Jacket', 'Apparel', 'Wrong Size', 'Like New', 'Eligible', 0.95, 'Chennai', DATEADD(DAY,-87,@Now), DATEADD(DAY,-87,@Now), 0),
('30000000-0003-0001-0001-000000000002', 'PROD-ADID-001', 'Adidas Shoes', 'Apparel', 'Changed Mind', 'Good', 'Eligible', 0.88, 'Chennai', DATEADD(DAY,-84,@Now), DATEADD(DAY,-84,@Now), 0),
('30000000-0003-0001-0001-000000000003', 'PROD-APPL-001', 'Apple AirPods', 'Electronics', 'Defective', 'Damaged', 'Not Eligible', 0.92, 'Chennai', DATEADD(DAY,-81,@Now), DATEADD(DAY,-81,@Now), 0),
('30000000-0003-0001-0001-000000000004', 'PROD-SAMS-001', 'Samsung Earbuds', 'Electronics', 'Ordered Wrong Item', 'Like New', 'Eligible', 0.97, 'Chennai', DATEADD(DAY,-79,@Now), DATEADD(DAY,-79,@Now), 0),
('30000000-0003-0001-0001-000000000005', 'PROD-PUMA-001', 'Puma T-Shirt', 'Apparel', 'Color Mismatch', 'Fair', 'Eligible', 0.75, 'Chennai', DATEADD(DAY,-77,@Now), DATEADD(DAY,-77,@Now), 0),
('30000000-0003-0001-0001-000000000006', 'PROD-LEVI-001', 'Levis Jeans', 'Apparel', 'Wrong Size', 'Good', 'Eligible', 0.89, 'Chennai', DATEADD(DAY,-74,@Now), DATEADD(DAY,-74,@Now), 0),
('30000000-0003-0001-0001-000000000007', 'PROD-DELL-001', 'Dell Mouse', 'Electronics', 'Changed Mind', 'Like New', 'Eligible', 0.96, 'Chennai', DATEADD(DAY,-72,@Now), DATEADD(DAY,-72,@Now), 0),
('30000000-0003-0001-0001-000000000008', 'PROD-LOGI-001', 'Logitech Keyboard', 'Electronics', 'Packaging Damaged', 'Damaged', 'Not Eligible', 0.85, 'Chennai', DATEADD(DAY,-69,@Now), DATEADD(DAY,-69,@Now), 0),
-- Bangalore (8 records: 6 eligible, 2 not)
('30000000-0003-0001-0001-000000000009', 'PROD-NIKE-001', 'Nike Jacket', 'Apparel', 'Wrong Size', 'Good', 'Eligible', 0.87, 'Bangalore', DATEADD(DAY,-67,@Now), DATEADD(DAY,-67,@Now), 0),
('30000000-0003-0001-0001-000000000010', 'PROD-ADID-001', 'Adidas Shoes', 'Apparel', 'Defective', 'Fair', 'Eligible', 0.72, 'Bangalore', DATEADD(DAY,-65,@Now), DATEADD(DAY,-65,@Now), 0),
('30000000-0003-0001-0001-000000000011', 'PROD-APPL-001', 'Apple AirPods', 'Electronics', 'Changed Mind', 'Like New', 'Eligible', 0.98, 'Bangalore', DATEADD(DAY,-63,@Now), DATEADD(DAY,-63,@Now), 0),
('30000000-0003-0001-0001-000000000012', 'PROD-SAMS-001', 'Samsung Earbuds', 'Electronics', 'Ordered Wrong Item', 'Good', 'Eligible', 0.84, 'Bangalore', DATEADD(DAY,-61,@Now), DATEADD(DAY,-61,@Now), 0),
('30000000-0003-0001-0001-000000000013', 'PROD-PUMA-001', 'Puma T-Shirt', 'Apparel', 'Color Mismatch', 'Like New', 'Eligible', 0.94, 'Bangalore', DATEADD(DAY,-59,@Now), DATEADD(DAY,-59,@Now), 0),
('30000000-0003-0001-0001-000000000014', 'PROD-LEVI-001', 'Levis Jeans', 'Apparel', 'Wrong Size', 'Good', 'Eligible', 0.86, 'Bangalore', DATEADD(DAY,-57,@Now), DATEADD(DAY,-57,@Now), 0),
('30000000-0003-0001-0001-000000000015', 'PROD-HP-001', 'HP Backpack', 'Accessories', 'Packaging Damaged', 'Damaged', 'Not Eligible', 0.91, 'Bangalore', DATEADD(DAY,-55,@Now), DATEADD(DAY,-55,@Now), 0),
('30000000-0003-0001-0001-000000000016', 'PROD-ECHO-001', 'Amazon Echo', 'Electronics', 'Changed Mind', 'Like New', 'Eligible', 0.96, 'Bangalore', DATEADD(DAY,-53,@Now), DATEADD(DAY,-53,@Now), 0),
-- Hyderabad (8 records: 6 eligible, 2 not)
('30000000-0003-0001-0001-000000000017', 'PROD-NIKE-001', 'Nike Jacket', 'Apparel', 'Wrong Size', 'Good', 'Eligible', 0.88, 'Hyderabad', DATEADD(DAY,-51,@Now), DATEADD(DAY,-51,@Now), 0),
('30000000-0003-0001-0001-000000000018', 'PROD-ADID-001', 'Adidas Shoes', 'Apparel', 'Defective', 'Fair', 'Eligible', 0.71, 'Hyderabad', DATEADD(DAY,-49,@Now), DATEADD(DAY,-49,@Now), 0),
('30000000-0003-0001-0001-000000000019', 'PROD-APPL-001', 'Apple AirPods', 'Electronics', 'Ordered Wrong Item', 'Like New', 'Eligible', 0.97, 'Hyderabad', DATEADD(DAY,-47,@Now), DATEADD(DAY,-47,@Now), 0),
('30000000-0003-0001-0001-000000000020', 'PROD-SAMS-001', 'Samsung Earbuds', 'Electronics', 'Color Mismatch', 'Damaged', 'Not Eligible', 0.90, 'Hyderabad', DATEADD(DAY,-45,@Now), DATEADD(DAY,-45,@Now), 0),
('30000000-0003-0001-0001-000000000021', 'PROD-DELL-001', 'Dell Mouse', 'Electronics', 'Changed Mind', 'Like New', 'Eligible', 0.95, 'Hyderabad', DATEADD(DAY,-43,@Now), DATEADD(DAY,-43,@Now), 0),
('30000000-0003-0001-0001-000000000022', 'PROD-LOGI-001', 'Logitech Keyboard', 'Electronics', 'Wrong Size', 'Good', 'Eligible', 0.83, 'Hyderabad', DATEADD(DAY,-41,@Now), DATEADD(DAY,-41,@Now), 0),
('30000000-0003-0001-0001-000000000023', 'PROD-HP-001', 'HP Backpack', 'Accessories', 'Packaging Damaged', 'Damaged', 'Not Eligible', 0.89, 'Hyderabad', DATEADD(DAY,-39,@Now), DATEADD(DAY,-39,@Now), 0),
('30000000-0003-0001-0001-000000000024', 'PROD-ECHO-001', 'Amazon Echo', 'Electronics', 'Defective', 'Fair', 'Eligible', 0.74, 'Hyderabad', DATEADD(DAY,-37,@Now), DATEADD(DAY,-37,@Now), 0),
-- Mumbai (8 records: 6 eligible, 2 not)
('30000000-0003-0001-0001-000000000025', 'PROD-NIKE-001', 'Nike Jacket', 'Apparel', 'Wrong Size', 'Like New', 'Eligible', 0.96, 'Mumbai', DATEADD(DAY,-35,@Now), DATEADD(DAY,-35,@Now), 0),
('30000000-0003-0001-0001-000000000026', 'PROD-ADID-001', 'Adidas Shoes', 'Apparel', 'Changed Mind', 'Good', 'Eligible', 0.85, 'Mumbai', DATEADD(DAY,-33,@Now), DATEADD(DAY,-33,@Now), 0),
('30000000-0003-0001-0001-000000000027', 'PROD-APPL-001', 'Apple AirPods', 'Electronics', 'Ordered Wrong Item', 'Like New', 'Eligible', 0.98, 'Mumbai', DATEADD(DAY,-31,@Now), DATEADD(DAY,-31,@Now), 0),
('30000000-0003-0001-0001-000000000028', 'PROD-SAMS-001', 'Samsung Earbuds', 'Electronics', 'Defective', 'Damaged', 'Not Eligible', 0.93, 'Mumbai', DATEADD(DAY,-29,@Now), DATEADD(DAY,-29,@Now), 0),
('30000000-0003-0001-0001-000000000029', 'PROD-PUMA-001', 'Puma T-Shirt', 'Apparel', 'Color Mismatch', 'Good', 'Eligible', 0.82, 'Mumbai', DATEADD(DAY,-27,@Now), DATEADD(DAY,-27,@Now), 0),
('30000000-0003-0001-0001-000000000030', 'PROD-LEVI-001', 'Levis Jeans', 'Apparel', 'Wrong Size', 'Like New', 'Eligible', 0.94, 'Mumbai', DATEADD(DAY,-25,@Now), DATEADD(DAY,-25,@Now), 0),
('30000000-0003-0001-0001-000000000031', 'PROD-DELL-001', 'Dell Mouse', 'Electronics', 'Changed Mind', 'Like New', 'Eligible', 0.97, 'Mumbai', DATEADD(DAY,-23,@Now), DATEADD(DAY,-23,@Now), 0),
('30000000-0003-0001-0001-000000000032', 'PROD-ECHO-001', 'Amazon Echo', 'Electronics', 'Packaging Damaged', 'Damaged', 'Not Eligible', 0.88, 'Mumbai', DATEADD(DAY,-21,@Now), DATEADD(DAY,-21,@Now), 0),
-- Delhi (8 records: 6 eligible, 2 not)
('30000000-0003-0001-0001-000000000033', 'PROD-NIKE-001', 'Nike Jacket', 'Apparel', 'Wrong Size', 'Good', 'Eligible', 0.86, 'Delhi', DATEADD(DAY,-19,@Now), DATEADD(DAY,-19,@Now), 0),
('30000000-0003-0001-0001-000000000034', 'PROD-ADID-001', 'Adidas Shoes', 'Apparel', 'Defective', 'Fair', 'Eligible', 0.73, 'Delhi', DATEADD(DAY,-17,@Now), DATEADD(DAY,-17,@Now), 0),
('30000000-0003-0001-0001-000000000035', 'PROD-APPL-001', 'Apple AirPods', 'Electronics', 'Ordered Wrong Item', 'Like New', 'Eligible', 0.99, 'Delhi', DATEADD(DAY,-15,@Now), DATEADD(DAY,-15,@Now), 0),
('30000000-0003-0001-0001-000000000036', 'PROD-SAMS-001', 'Samsung Earbuds', 'Electronics', 'Color Mismatch', 'Good', 'Eligible', 0.81, 'Delhi', DATEADD(DAY,-13,@Now), DATEADD(DAY,-13,@Now), 0),
('30000000-0003-0001-0001-000000000037', 'PROD-PUMA-001', 'Puma T-Shirt', 'Apparel', 'Changed Mind', 'Like New', 'Eligible', 0.93, 'Delhi', DATEADD(DAY,-11,@Now), DATEADD(DAY,-11,@Now), 0),
('30000000-0003-0001-0001-000000000038', 'PROD-LEVI-001', 'Levis Jeans', 'Apparel', 'Wrong Size', 'Good', 'Eligible', 0.87, 'Delhi', DATEADD(DAY,-9,@Now), DATEADD(DAY,-9,@Now), 0),
('30000000-0003-0001-0001-000000000039', 'PROD-LOGI-001', 'Logitech Keyboard', 'Electronics', 'Defective', 'Damaged', 'Not Eligible', 0.90, 'Delhi', DATEADD(DAY,-7,@Now), DATEADD(DAY,-7,@Now), 0),
('30000000-0003-0001-0001-000000000040', 'PROD-HP-001', 'HP Backpack', 'Accessories', 'Packaging Damaged', 'Damaged', 'Not Eligible', 0.92, 'Delhi', DATEADD(DAY,-5,@Now), DATEADD(DAY,-5,@Now), 0),
-- Pune (10 records: 7 eligible, 3 not)
('30000000-0003-0001-0001-000000000041', 'PROD-NIKE-001', 'Nike Jacket', 'Apparel', 'Wrong Size', 'Like New', 'Eligible', 0.95, 'Pune', DATEADD(DAY,-4,@Now), DATEADD(DAY,-4,@Now), 0),
('30000000-0003-0001-0001-000000000042', 'PROD-ADID-001', 'Adidas Shoes', 'Apparel', 'Changed Mind', 'Good', 'Eligible', 0.84, 'Pune', DATEADD(DAY,-3,@Now), DATEADD(DAY,-3,@Now), 0),
('30000000-0003-0001-0001-000000000043', 'PROD-APPL-001', 'Apple AirPods', 'Electronics', 'Ordered Wrong Item', 'Like New', 'Eligible', 0.98, 'Pune', DATEADD(DAY,-3,@Now), DATEADD(DAY,-3,@Now), 0),
('30000000-0003-0001-0001-000000000044', 'PROD-SAMS-001', 'Samsung Earbuds', 'Electronics', 'Defective', 'Fair', 'Eligible', 0.76, 'Pune', DATEADD(DAY,-1,@Now), DATEADD(DAY,-1,@Now), 0),
('30000000-0003-0001-0001-000000000045', 'PROD-PUMA-001', 'Puma T-Shirt', 'Apparel', 'Color Mismatch', 'Like New', 'Eligible', 0.93, 'Pune', DATEADD(DAY,-2,@Now), DATEADD(DAY,-2,@Now), 0),
('30000000-0003-0001-0001-000000000046', 'PROD-LEVI-001', 'Levis Jeans', 'Apparel', 'Wrong Size', 'Good', 'Eligible', 0.86, 'Pune', DATEADD(DAY,-1,@Now), DATEADD(DAY,-1,@Now), 0),
('30000000-0003-0001-0001-000000000047', 'PROD-DELL-001', 'Dell Mouse', 'Electronics', 'Changed Mind', 'Like New', 'Eligible', 0.97, 'Pune', DATEADD(DAY,-2,@Now), DATEADD(DAY,-2,@Now), 0),
('30000000-0003-0001-0001-000000000048', 'PROD-LOGI-001', 'Logitech Keyboard', 'Electronics', 'Packaging Damaged', 'Damaged', 'Not Eligible', 0.88, 'Pune', DATEADD(DAY,-3,@Now), DATEADD(DAY,-3,@Now), 0),
('30000000-0003-0001-0001-000000000049', 'PROD-HP-001', 'HP Backpack', 'Accessories', 'Defective', 'Fair', 'Eligible', 0.70, 'Pune', DATEADD(DAY,-3,@Now), DATEADD(DAY,-3,@Now), 0),
('30000000-0003-0001-0001-000000000050', 'PROD-ECHO-001', 'Amazon Echo', 'Electronics', 'Wrong Size', 'Like New', 'Eligible', 0.96, 'Pune', DATEADD(DAY,-5,@Now), DATEADD(DAY,-5,@Now), 0);
GO

-- ============================================================
-- SECTION 4: InventoryPool (35 records - only Eligible returns)
-- ~60% of these will be 'Matched', rest 'Available'
-- MatchScore: 50-100
-- ============================================================
DECLARE @Now DATETIME2 = SYSUTCDATETIME();

INSERT INTO [dbo].[InventoryPool]
	([Id], [ReturnId], [ProductId], [Location], [HoldingDays], [MatchScore], [Status], [CreatedAt], [IsDeleted])
VALUES
-- Chennai eligible (6 items: 4 matched, 2 available)
('40000000-0004-0001-0001-000000000001', '30000000-0003-0001-0001-000000000001', 'PROD-NIKE-001', 'Chennai', 3, 92.5, 'Matched', DATEADD(DAY,-86,@Now), 0),
('40000000-0004-0001-0001-000000000002', '30000000-0003-0001-0001-000000000002', 'PROD-ADID-001', 'Chennai', 5, 78.3, 'Available', DATEADD(DAY,-83,@Now), 0),
('40000000-0004-0001-0001-000000000004', '30000000-0003-0001-0001-000000000004', 'PROD-SAMS-001', 'Chennai', 2, 95.1, 'Matched', DATEADD(DAY,-78,@Now), 0),
('40000000-0004-0001-0001-000000000005', '30000000-0003-0001-0001-000000000005', 'PROD-PUMA-001', 'Chennai', 7, 65.2, 'Available', DATEADD(DAY,-76,@Now), 0),
('40000000-0004-0001-0001-000000000006', '30000000-0003-0001-0001-000000000006', 'PROD-LEVI-001', 'Chennai', 4, 88.7, 'Matched', DATEADD(DAY,-73,@Now), 0),
('40000000-0004-0001-0001-000000000007', '30000000-0003-0001-0001-000000000007', 'PROD-DELL-001', 'Chennai', 6, 72.4, 'Available', DATEADD(DAY,-71,@Now), 0),
-- Bangalore eligible (6 items: 4 matched, 2 available)
('40000000-0004-0001-0001-000000000009', '30000000-0003-0001-0001-000000000009', 'PROD-NIKE-001', 'Bangalore', 3, 89.6, 'Matched', DATEADD(DAY,-66,@Now), 0),
('40000000-0004-0001-0001-000000000010', '30000000-0003-0001-0001-000000000010', 'PROD-ADID-001', 'Bangalore', 8, 58.9, 'Available', DATEADD(DAY,-64,@Now), 0),
('40000000-0004-0001-0001-000000000011', '30000000-0003-0001-0001-000000000011', 'PROD-APPL-001', 'Bangalore', 1, 97.8, 'Matched', DATEADD(DAY,-62,@Now), 0),
('40000000-0004-0001-0001-000000000012', '30000000-0003-0001-0001-000000000012', 'PROD-SAMS-001', 'Bangalore', 5, 74.5, 'Available', DATEADD(DAY,-60,@Now), 0),
('40000000-0004-0001-0001-000000000013', '30000000-0003-0001-0001-000000000013', 'PROD-PUMA-001', 'Bangalore', 2, 91.3, 'Matched', DATEADD(DAY,-58,@Now), 0),
('40000000-0004-0001-0001-000000000016', '30000000-0003-0001-0001-000000000016', 'PROD-ECHO-001', 'Bangalore', 1, 96.2, 'Matched', DATEADD(DAY,-52,@Now), 0),
-- Hyderabad eligible (5 items: 3 matched, 2 available)
('40000000-0004-0001-0001-000000000017', '30000000-0003-0001-0001-000000000017', 'PROD-NIKE-001', 'Hyderabad', 4, 85.4, 'Matched', DATEADD(DAY,-50,@Now), 0),
('40000000-0004-0001-0001-000000000018', '30000000-0003-0001-0001-000000000018', 'PROD-ADID-001', 'Hyderabad', 9, 55.7, 'Available', DATEADD(DAY,-48,@Now), 0),
('40000000-0004-0001-0001-000000000019', '30000000-0003-0001-0001-000000000019', 'PROD-APPL-001', 'Hyderabad', 1, 98.2, 'Matched', DATEADD(DAY,-46,@Now), 0),
('40000000-0004-0001-0001-000000000021', '30000000-0003-0001-0001-000000000021', 'PROD-DELL-001', 'Hyderabad', 6, 69.8, 'Available', DATEADD(DAY,-42,@Now), 0),
('40000000-0004-0001-0001-000000000022', '30000000-0003-0001-0001-000000000022', 'PROD-LOGI-001', 'Hyderabad', 3, 87.1, 'Matched', DATEADD(DAY,-40,@Now), 0),
('40000000-0004-0001-0001-000000000024', '30000000-0003-0001-0001-000000000024', 'PROD-ECHO-001', 'Hyderabad', 7, 62.3, 'Available', DATEADD(DAY,-36,@Now), 0),
-- Mumbai eligible (6 items: 4 matched, 2 available)
('40000000-0004-0001-0001-000000000025', '30000000-0003-0001-0001-000000000025', 'PROD-NIKE-001', 'Mumbai', 2, 93.7, 'Matched', DATEADD(DAY,-34,@Now), 0),
('40000000-0004-0001-0001-000000000026', '30000000-0003-0001-0001-000000000026', 'PROD-ADID-001', 'Mumbai', 6, 71.2, 'Available', DATEADD(DAY,-32,@Now), 0),
('40000000-0004-0001-0001-000000000027', '30000000-0003-0001-0001-000000000027', 'PROD-APPL-001', 'Mumbai', 1, 99.1, 'Matched', DATEADD(DAY,-30,@Now), 0),
('40000000-0004-0001-0001-000000000029', '30000000-0003-0001-0001-000000000029', 'PROD-PUMA-001', 'Mumbai', 5, 67.8, 'Available', DATEADD(DAY,-26,@Now), 0),
('40000000-0004-0001-0001-000000000030', '30000000-0003-0001-0001-000000000030', 'PROD-LEVI-001', 'Mumbai', 3, 90.5, 'Matched', DATEADD(DAY,-24,@Now), 0),
('40000000-0004-0001-0001-000000000031', '30000000-0003-0001-0001-000000000031', 'PROD-DELL-001', 'Mumbai', 4, 76.9, 'Matched', DATEADD(DAY,-22,@Now), 0),
-- Delhi eligible (5 items: 3 matched, 2 available)
('40000000-0004-0001-0001-000000000033', '30000000-0003-0001-0001-000000000033', 'PROD-NIKE-001', 'Delhi', 3, 86.3, 'Matched', DATEADD(DAY,-18,@Now), 0),
('40000000-0004-0001-0001-000000000034', '30000000-0003-0001-0001-000000000034', 'PROD-ADID-001', 'Delhi', 7, 57.4, 'Available', DATEADD(DAY,-16,@Now), 0),
('40000000-0004-0001-0001-000000000035', '30000000-0003-0001-0001-000000000035', 'PROD-APPL-001', 'Delhi', 1, 98.9, 'Matched', DATEADD(DAY,-14,@Now), 0),
('40000000-0004-0001-0001-000000000036', '30000000-0003-0001-0001-000000000036', 'PROD-SAMS-001', 'Delhi', 5, 73.6, 'Available', DATEADD(DAY,-12,@Now), 0),
('40000000-0004-0001-0001-000000000037', '30000000-0003-0001-0001-000000000037', 'PROD-PUMA-001', 'Delhi', 2, 91.8, 'Matched', DATEADD(DAY,-10,@Now), 0),
('40000000-0004-0001-0001-000000000038', '30000000-0003-0001-0001-000000000038', 'PROD-LEVI-001', 'Delhi', 4, 82.1, 'Available', DATEADD(DAY,-8,@Now), 0),
-- Pune eligible (7 items: 5 matched, 2 available)
('40000000-0004-0001-0001-000000000041', '30000000-0003-0001-0001-000000000041', 'PROD-NIKE-001', 'Pune', 1, 94.6, 'Matched', DATEADD(DAY,-3,@Now), 0),
('40000000-0004-0001-0001-000000000042', '30000000-0003-0001-0001-000000000042', 'PROD-ADID-001', 'Pune', 4, 68.5, 'Available', DATEADD(DAY,-2,@Now), 0),
('40000000-0004-0001-0001-000000000043', '30000000-0003-0001-0001-000000000043', 'PROD-APPL-001', 'Pune', 1, 97.4, 'Matched', DATEADD(DAY,-2,@Now), 0),
('40000000-0004-0001-0001-000000000045', '30000000-0003-0001-0001-000000000045', 'PROD-PUMA-001', 'Pune', 2, 90.2, 'Matched', DATEADD(DAY,-1,@Now), 0),
('40000000-0004-0001-0001-000000000047', '30000000-0003-0001-0001-000000000047', 'PROD-DELL-001', 'Pune', 1, 93.1, 'Matched', DATEADD(DAY,-1,@Now), 0),
('40000000-0004-0001-0001-000000000050', '30000000-0003-0001-0001-000000000050', 'PROD-ECHO-001', 'Pune', 2, 88.9, 'Matched', DATEADD(DAY,-4,@Now), 0);
GO

-- ============================================================
-- SECTION 5: DemandHistory (60 records - 10 products x 6 locations)
-- DemandScore: 30-100, OrderCount: 5-500
-- ============================================================
DECLARE @Now DATETIME2 = SYSUTCDATETIME();

INSERT INTO [dbo].[DemandHistory]
	([Id], [ProductId], [Region], [OrderCount], [DemandScore], [CreatedAt], [IsDeleted])
VALUES
-- Nike Jacket
('50000000-0005-0001-0001-000000000001', 'PROD-NIKE-001', 'Chennai', 180, 78.5, DATEADD(DAY,-60,@Now), 0),
('50000000-0005-0001-0001-000000000002', 'PROD-NIKE-001', 'Bangalore', 250, 88.2, DATEADD(DAY,-60,@Now), 0),
('50000000-0005-0001-0001-000000000003', 'PROD-NIKE-001', 'Hyderabad', 145, 72.1, DATEADD(DAY,-60,@Now), 0),
('50000000-0005-0001-0001-000000000004', 'PROD-NIKE-001', 'Mumbai', 320, 94.7, DATEADD(DAY,-60,@Now), 0),
('50000000-0005-0001-0001-000000000005', 'PROD-NIKE-001', 'Delhi', 290, 91.3, DATEADD(DAY,-60,@Now), 0),
('50000000-0005-0001-0001-000000000006', 'PROD-NIKE-001', 'Pune', 160, 75.8, DATEADD(DAY,-60,@Now), 0),
-- Adidas Shoes
('50000000-0005-0001-0001-000000000007', 'PROD-ADID-001', 'Chennai', 120, 65.3, DATEADD(DAY,-55,@Now), 0),
('50000000-0005-0001-0001-000000000008', 'PROD-ADID-001', 'Bangalore', 200, 82.6, DATEADD(DAY,-55,@Now), 0),
('50000000-0005-0001-0001-000000000009', 'PROD-ADID-001', 'Hyderabad', 95, 55.4, DATEADD(DAY,-55,@Now), 0),
('50000000-0005-0001-0001-000000000010', 'PROD-ADID-001', 'Mumbai', 275, 89.1, DATEADD(DAY,-55,@Now), 0),
('50000000-0005-0001-0001-000000000011', 'PROD-ADID-001', 'Delhi', 230, 85.7, DATEADD(DAY,-55,@Now), 0),
('50000000-0005-0001-0001-000000000012', 'PROD-ADID-001', 'Pune', 110, 61.2, DATEADD(DAY,-55,@Now), 0),
-- Apple AirPods
('50000000-0005-0001-0001-000000000013', 'PROD-APPL-001', 'Chennai', 350, 96.8, DATEADD(DAY,-50,@Now), 0),
('50000000-0005-0001-0001-000000000014', 'PROD-APPL-001', 'Bangalore', 420, 99.2, DATEADD(DAY,-50,@Now), 0),
('50000000-0005-0001-0001-000000000015', 'PROD-APPL-001', 'Hyderabad', 280, 90.5, DATEADD(DAY,-50,@Now), 0),
('50000000-0005-0001-0001-000000000016', 'PROD-APPL-001', 'Mumbai', 500, 100.0, DATEADD(DAY,-50,@Now), 0),
('50000000-0005-0001-0001-000000000017', 'PROD-APPL-001', 'Delhi', 450, 98.5, DATEADD(DAY,-50,@Now), 0),
('50000000-0005-0001-0001-000000000018', 'PROD-APPL-001', 'Pune', 310, 93.4, DATEADD(DAY,-50,@Now), 0),
-- Samsung Earbuds
('50000000-0005-0001-0001-000000000019', 'PROD-SAMS-001', 'Chennai', 200, 80.2, DATEADD(DAY,-45,@Now), 0),
('50000000-0005-0001-0001-000000000020', 'PROD-SAMS-001', 'Bangalore', 175, 76.8, DATEADD(DAY,-45,@Now), 0),
('50000000-0005-0001-0001-000000000021', 'PROD-SAMS-001', 'Hyderabad', 130, 68.3, DATEADD(DAY,-45,@Now), 0),
('50000000-0005-0001-0001-000000000022', 'PROD-SAMS-001', 'Mumbai', 240, 84.9, DATEADD(DAY,-45,@Now), 0),
('50000000-0005-0001-0001-000000000023', 'PROD-SAMS-001', 'Delhi', 210, 81.5, DATEADD(DAY,-45,@Now), 0),
('50000000-0005-0001-0001-000000000024', 'PROD-SAMS-001', 'Pune', 105, 59.7, DATEADD(DAY,-45,@Now), 0),
-- Puma T-Shirt
('50000000-0005-0001-0001-000000000025', 'PROD-PUMA-001', 'Chennai', 85, 48.6, DATEADD(DAY,-40,@Now), 0),
('50000000-0005-0001-0001-000000000026', 'PROD-PUMA-001', 'Bangalore', 150, 72.4, DATEADD(DAY,-40,@Now), 0),
('50000000-0005-0001-0001-000000000027', 'PROD-PUMA-001', 'Hyderabad', 70, 42.1, DATEADD(DAY,-40,@Now), 0),
('50000000-0005-0001-0001-000000000028', 'PROD-PUMA-001', 'Mumbai', 190, 79.3, DATEADD(DAY,-40,@Now), 0),
('50000000-0005-0001-0001-000000000029', 'PROD-PUMA-001', 'Delhi', 165, 74.8, DATEADD(DAY,-40,@Now), 0),
('50000000-0005-0001-0001-000000000030', 'PROD-PUMA-001', 'Pune', 95, 52.9, DATEADD(DAY,-40,@Now), 0),
-- Levis Jeans
('50000000-0005-0001-0001-000000000031', 'PROD-LEVI-001', 'Chennai', 140, 70.1, DATEADD(DAY,-35,@Now), 0),
('50000000-0005-0001-0001-000000000032', 'PROD-LEVI-001', 'Bangalore', 185, 77.9, DATEADD(DAY,-35,@Now), 0),
('50000000-0005-0001-0001-000000000033', 'PROD-LEVI-001', 'Hyderabad', 110, 62.5, DATEADD(DAY,-35,@Now), 0),
('50000000-0005-0001-0001-000000000034', 'PROD-LEVI-001', 'Mumbai', 260, 87.3, DATEADD(DAY,-35,@Now), 0),
('50000000-0005-0001-0001-000000000035', 'PROD-LEVI-001', 'Delhi', 220, 83.6, DATEADD(DAY,-35,@Now), 0),
('50000000-0005-0001-0001-000000000036', 'PROD-LEVI-001', 'Pune', 125, 66.4, DATEADD(DAY,-35,@Now), 0),
-- Dell Mouse
('50000000-0005-0001-0001-000000000037', 'PROD-DELL-001', 'Chennai', 90, 50.8, DATEADD(DAY,-30,@Now), 0),
('50000000-0005-0001-0001-000000000038', 'PROD-DELL-001', 'Bangalore', 130, 67.2, DATEADD(DAY,-30,@Now), 0),
('50000000-0005-0001-0001-000000000039', 'PROD-DELL-001', 'Hyderabad', 75, 44.6, DATEADD(DAY,-30,@Now), 0),
('50000000-0005-0001-0001-000000000040', 'PROD-DELL-001', 'Mumbai', 160, 74.1, DATEADD(DAY,-30,@Now), 0),
('50000000-0005-0001-0001-000000000041', 'PROD-DELL-001', 'Delhi', 145, 71.3, DATEADD(DAY,-30,@Now), 0),
('50000000-0005-0001-0001-000000000042', 'PROD-DELL-001', 'Pune', 100, 56.7, DATEADD(DAY,-30,@Now), 0),
-- Logitech Keyboard
('50000000-0005-0001-0001-000000000043', 'PROD-LOGI-001', 'Chennai', 80, 46.3, DATEADD(DAY,-25,@Now), 0),
('50000000-0005-0001-0001-000000000044', 'PROD-LOGI-001', 'Bangalore', 115, 63.8, DATEADD(DAY,-25,@Now), 0),
('50000000-0005-0001-0001-000000000045', 'PROD-LOGI-001', 'Hyderabad', 95, 54.2, DATEADD(DAY,-25,@Now), 0),
('50000000-0005-0001-0001-000000000046', 'PROD-LOGI-001', 'Mumbai', 140, 69.5, DATEADD(DAY,-25,@Now), 0),
('50000000-0005-0001-0001-000000000047', 'PROD-LOGI-001', 'Delhi', 125, 65.9, DATEADD(DAY,-25,@Now), 0),
('50000000-0005-0001-0001-000000000048', 'PROD-LOGI-001', 'Pune', 70, 40.1, DATEADD(DAY,-25,@Now), 0),
-- HP Backpack
('50000000-0005-0001-0001-000000000049', 'PROD-HP-001', 'Chennai', 55, 35.7, DATEADD(DAY,-20,@Now), 0),
('50000000-0005-0001-0001-000000000050', 'PROD-HP-001', 'Bangalore', 90, 51.4, DATEADD(DAY,-20,@Now), 0),
('50000000-0005-0001-0001-000000000051', 'PROD-HP-001', 'Hyderabad', 65, 39.8, DATEADD(DAY,-20,@Now), 0),
('50000000-0005-0001-0001-000000000052', 'PROD-HP-001', 'Mumbai', 120, 64.2, DATEADD(DAY,-20,@Now), 0),
('50000000-0005-0001-0001-000000000053', 'PROD-HP-001', 'Delhi', 100, 57.6, DATEADD(DAY,-20,@Now), 0),
('50000000-0005-0001-0001-000000000054', 'PROD-HP-001', 'Pune', 45, 32.4, DATEADD(DAY,-20,@Now), 0),
-- Amazon Echo
('50000000-0005-0001-0001-000000000055', 'PROD-ECHO-001', 'Chennai', 220, 83.1, DATEADD(DAY,-15,@Now), 0),
('50000000-0005-0001-0001-000000000056', 'PROD-ECHO-001', 'Bangalore', 300, 92.7, DATEADD(DAY,-15,@Now), 0),
('50000000-0005-0001-0001-000000000057', 'PROD-ECHO-001', 'Hyderabad', 180, 77.4, DATEADD(DAY,-15,@Now), 0),
('50000000-0005-0001-0001-000000000058', 'PROD-ECHO-001', 'Mumbai', 350, 95.6, DATEADD(DAY,-15,@Now), 0),
('50000000-0005-0001-0001-000000000059', 'PROD-ECHO-001', 'Delhi', 280, 89.8, DATEADD(DAY,-15,@Now), 0),
('50000000-0005-0001-0001-000000000060', 'PROD-ECHO-001', 'Pune', 195, 80.3, DATEADD(DAY,-15,@Now), 0);
GO

-- ============================================================
-- SECTION 6: AgentRecommendations
-- 3 agents: ImageValidationAgent, MatchAgent, RootCauseAgent
-- ============================================================
DECLARE @Now DATETIME2 = SYSUTCDATETIME();

INSERT INTO [dbo].[AgentRecommendations]
	([Id], [AgentName], [Recommendation], [Confidence], [CreatedDate], [CreatedAt], [IsDeleted])
VALUES
-- ImageValidationAgent recommendations
('60000000-0006-0001-0001-000000000001', 'ImageValidationAgent', 'Item validated as Like New. No visible damage. Eligible for hyperlocal matching.', 0.95, DATEADD(DAY,-87,@Now), DATEADD(DAY,-87,@Now), 0),
('60000000-0006-0001-0001-000000000002', 'ImageValidationAgent', 'Item shows minor wear on edges. Condition: Good. Eligible for inventory pool.', 0.88, DATEADD(DAY,-84,@Now), DATEADD(DAY,-84,@Now), 0),
('60000000-0006-0001-0001-000000000003', 'ImageValidationAgent', 'Significant damage detected. Cracked screen visible. Not eligible for reuse.', 0.92, DATEADD(DAY,-81,@Now), DATEADD(DAY,-81,@Now), 0),
('60000000-0006-0001-0001-000000000004', 'ImageValidationAgent', 'Item in pristine condition. Original packaging intact. Like New eligible.', 0.97, DATEADD(DAY,-79,@Now), DATEADD(DAY,-79,@Now), 0),
('60000000-0006-0001-0001-000000000005', 'ImageValidationAgent', 'Light discoloration noted. Condition: Fair. Still eligible for pool.', 0.75, DATEADD(DAY,-77,@Now), DATEADD(DAY,-77,@Now), 0),
('60000000-0006-0001-0001-000000000006', 'ImageValidationAgent', 'Item shows normal use signs. Good condition. Eligible.', 0.89, DATEADD(DAY,-74,@Now), DATEADD(DAY,-74,@Now), 0),
('60000000-0006-0001-0001-000000000007', 'ImageValidationAgent', 'Like New condition confirmed. All accessories present.', 0.96, DATEADD(DAY,-72,@Now), DATEADD(DAY,-72,@Now), 0),
('60000000-0006-0001-0001-000000000008', 'ImageValidationAgent', 'Severe packaging crush damage. Internal components likely affected. Rejected.', 0.85, DATEADD(DAY,-69,@Now), DATEADD(DAY,-69,@Now), 0),
('60000000-0006-0001-0001-000000000009', 'ImageValidationAgent', 'Good condition jacket. Minor fold marks only. Eligible.', 0.87, DATEADD(DAY,-67,@Now), DATEADD(DAY,-67,@Now), 0),
('60000000-0006-0001-0001-000000000010', 'ImageValidationAgent', 'Fair condition. Sole shows wear but functional. Eligible.', 0.72, DATEADD(DAY,-65,@Now), DATEADD(DAY,-65,@Now), 0),
('60000000-0006-0001-0001-000000000011', 'ImageValidationAgent', 'Perfect condition AirPods. Sealed case. Like New.', 0.98, DATEADD(DAY,-63,@Now), DATEADD(DAY,-63,@Now), 0),
('60000000-0006-0001-0001-000000000012', 'ImageValidationAgent', 'Item validated. Good condition earbuds. Eligible for pool.', 0.84, DATEADD(DAY,-61,@Now), DATEADD(DAY,-61,@Now), 0),
('60000000-0006-0001-0001-000000000013', 'ImageValidationAgent', 'Like New T-shirt. Tags still attached. Eligible.', 0.94, DATEADD(DAY,-59,@Now), DATEADD(DAY,-59,@Now), 0),
('60000000-0006-0001-0001-000000000014', 'ImageValidationAgent', 'Good condition. No defects detected. Eligible.', 0.86, DATEADD(DAY,-57,@Now), DATEADD(DAY,-57,@Now), 0),
('60000000-0006-0001-0001-000000000015', 'ImageValidationAgent', 'Backpack strap torn. Zipper damaged. Not eligible.', 0.91, DATEADD(DAY,-55,@Now), DATEADD(DAY,-55,@Now), 0),
-- MatchAgent recommendations
('60000000-0006-0001-0001-000000000016', 'MatchAgent', 'Local match found in Chennai. Nike Jacket demand score 78.5. Distance saved: 120km.', 0.92, DATEADD(DAY,-86,@Now), DATEADD(DAY,-86,@Now), 0),
('60000000-0006-0001-0001-000000000017', 'MatchAgent', 'Matched Samsung Earbuds to Chennai buyer. High confidence. Cost saved: Rs.1,000.', 0.95, DATEADD(DAY,-78,@Now), DATEADD(DAY,-78,@Now), 0),
('60000000-0006-0001-0001-000000000018', 'MatchAgent', 'Levis Jeans matched locally. Demand strong in Chennai area. CO2 saved: 5.2kg.', 0.88, DATEADD(DAY,-73,@Now), DATEADD(DAY,-73,@Now), 0),
('60000000-0006-0001-0001-000000000019', 'MatchAgent', 'Nike Jacket matched in Bangalore. Score 89.6. Distance saved: 350km.', 0.89, DATEADD(DAY,-66,@Now), DATEADD(DAY,-66,@Now), 0),
('60000000-0006-0001-0001-000000000020', 'MatchAgent', 'Apple AirPods instant match. Highest demand product in Bangalore. Cost saved: Rs.3,600.', 0.97, DATEADD(DAY,-62,@Now), DATEADD(DAY,-62,@Now), 0),
('60000000-0006-0001-0001-000000000021', 'MatchAgent', 'Puma T-Shirt matched. Category demand high in Bangalore area.', 0.91, DATEADD(DAY,-58,@Now), DATEADD(DAY,-58,@Now), 0),
('60000000-0006-0001-0001-000000000022', 'MatchAgent', 'Amazon Echo matched in Bangalore. Electronics demand surge detected.', 0.96, DATEADD(DAY,-52,@Now), DATEADD(DAY,-52,@Now), 0),
('60000000-0006-0001-0001-000000000023', 'MatchAgent', 'Nike Jacket matched Hyderabad. Distance saved: 200km. Score: 85.4.', 0.85, DATEADD(DAY,-50,@Now), DATEADD(DAY,-50,@Now), 0),
('60000000-0006-0001-0001-000000000024', 'MatchAgent', 'AirPods matched instantly in Hyderabad. Demand score 90.5. Cost saved: Rs.3,000.', 0.98, DATEADD(DAY,-46,@Now), DATEADD(DAY,-46,@Now), 0),
('60000000-0006-0001-0001-000000000025', 'MatchAgent', 'Logitech Keyboard matched. Hyderabad IT corridor demand strong.', 0.87, DATEADD(DAY,-40,@Now), DATEADD(DAY,-40,@Now), 0),
('60000000-0006-0001-0001-000000000026', 'MatchAgent', 'Nike Jacket matched Mumbai. Premium area demand. Distance: 450km saved.', 0.93, DATEADD(DAY,-34,@Now), DATEADD(DAY,-34,@Now), 0),
('60000000-0006-0001-0001-000000000027', 'MatchAgent', 'AirPods matched Mumbai buyer. Highest score 99.1. CO2 saved: 12.8kg.', 0.99, DATEADD(DAY,-30,@Now), DATEADD(DAY,-30,@Now), 0),
('60000000-0006-0001-0001-000000000028', 'MatchAgent', 'Levis Jeans matched Mumbai. Strong apparel demand. Cost saved: Rs.2,200.', 0.90, DATEADD(DAY,-24,@Now), DATEADD(DAY,-24,@Now), 0),
('60000000-0006-0001-0001-000000000029', 'MatchAgent', 'Dell Mouse matched Mumbai. Electronics pool healthy.', 0.76, DATEADD(DAY,-22,@Now), DATEADD(DAY,-22,@Now), 0),
('60000000-0006-0001-0001-000000000030', 'MatchAgent', 'Nike Jacket matched Delhi. Score 86.3. Distance saved: 680km.', 0.86, DATEADD(DAY,-18,@Now), DATEADD(DAY,-18,@Now), 0),
('60000000-0006-0001-0001-000000000031', 'MatchAgent', 'AirPods matched Delhi NCR. Record demand. Cost saved: Rs.3,400.', 0.98, DATEADD(DAY,-14,@Now), DATEADD(DAY,-14,@Now), 0),
('60000000-0006-0001-0001-000000000032', 'MatchAgent', 'Puma T-Shirt matched Delhi. Apparel category trending.', 0.91, DATEADD(DAY,-10,@Now), DATEADD(DAY,-10,@Now), 0),
('60000000-0006-0001-0001-000000000033', 'MatchAgent', 'Nike Jacket matched Pune. New market. Distance saved: 150km.', 0.94, DATEADD(DAY,-3,@Now), DATEADD(DAY,-3,@Now), 0),
('60000000-0006-0001-0001-000000000034', 'MatchAgent', 'AirPods matched Pune IT hub. Score 97.4. Cost saved: Rs.2,800.', 0.97, DATEADD(DAY,-2,@Now), DATEADD(DAY,-2,@Now), 0),
('60000000-0006-0001-0001-000000000035', 'MatchAgent', 'Dell Mouse matched Pune. Rapid turnaround. CO2 saved: 3.2kg.', 0.93, DATEADD(DAY,-1,@Now), DATEADD(DAY,-1,@Now), 0),
-- RootCauseAgent recommendations
('60000000-0006-0001-0001-000000000036', 'RootCauseAgent', 'Pattern detected: Wrong Size accounts for 35% of Apparel returns in Chennai. Recommend size guide improvement.', 0.89, DATEADD(DAY,-80,@Now), DATEADD(DAY,-80,@Now), 0),
('60000000-0006-0001-0001-000000000037', 'RootCauseAgent', 'Defective returns spike in Electronics category in Chennai. Supplier quality review recommended.', 0.82, DATEADD(DAY,-70,@Now), DATEADD(DAY,-70,@Now), 0),
('60000000-0006-0001-0001-000000000038', 'RootCauseAgent', 'Changed Mind returns high in Bangalore for premium items. Consider better product descriptions.', 0.78, DATEADD(DAY,-60,@Now), DATEADD(DAY,-60,@Now), 0),
('60000000-0006-0001-0001-000000000039', 'RootCauseAgent', 'Color Mismatch returns concentrated in Apparel. Image accuracy audit recommended for listings.', 0.85, DATEADD(DAY,-50,@Now), DATEADD(DAY,-50,@Now), 0),
('60000000-0006-0001-0001-000000000040', 'RootCauseAgent', 'Packaging Damaged returns correlate with specific courier routes to Hyderabad. Route optimization needed.', 0.91, DATEADD(DAY,-40,@Now), DATEADD(DAY,-40,@Now), 0),
('60000000-0006-0001-0001-000000000041', 'RootCauseAgent', 'Ordered Wrong Item high in Electronics. Product comparison feature would reduce by estimated 20%.', 0.84, DATEADD(DAY,-30,@Now), DATEADD(DAY,-30,@Now), 0),
('60000000-0006-0001-0001-000000000042', 'RootCauseAgent', 'Mumbai shows highest return volume but also best match rate. Hyperlocal model thriving.', 0.92, DATEADD(DAY,-20,@Now), DATEADD(DAY,-20,@Now), 0),
('60000000-0006-0001-0001-000000000043', 'RootCauseAgent', 'Delhi NCR: Wrong Size + Apparel = 42% of returns. AR try-on feature recommended.', 0.87, DATEADD(DAY,-10,@Now), DATEADD(DAY,-10,@Now), 0),
('60000000-0006-0001-0001-000000000044', 'RootCauseAgent', 'Pune emerging market: low return volume, high eligibility rate (80%). Expand HIEN coverage.', 0.90, DATEADD(DAY,-5,@Now), DATEADD(DAY,-5,@Now), 0),
('60000000-0006-0001-0001-000000000045', 'RootCauseAgent', 'Cross-city analysis: AirPods have lowest return-to-reject ratio. Best product for HIEN model.', 0.95, DATEADD(DAY,-2,@Now), DATEADD(DAY,-2,@Now), 0);
GO

-- ============================================================
-- SECTION 7: VERIFICATION QUERIES
-- ============================================================
PRINT '========================================';
PRINT 'UPS ReLoop Nexus - Data Verification';
PRINT '========================================';

-- Total Packages
SELECT 'Total Packages' AS Metric, COUNT(*) AS [Value]
FROM [dbo].[Packages] WHERE [IsDeleted] = 0;

-- Total ReturnRequests
SELECT 'Total ReturnRequests' AS Metric, COUNT(*) AS [Value]
FROM [dbo].[ReturnRequests] WHERE [IsDeleted] = 0;

-- Total ImageValidationResults
SELECT 'Total ImageValidationResults' AS Metric, COUNT(*) AS [Value]
FROM [dbo].[ImageValidationResults] WHERE [IsDeleted] = 0;

-- Eligible Returns
SELECT 'Eligible Returns' AS Metric, COUNT(*) AS [Value]
FROM [dbo].[ImageValidationResults]
WHERE [IsDeleted] = 0 AND [Eligibility] = 'Eligible';

-- Not Eligible Returns
SELECT 'Not Eligible Returns' AS Metric, COUNT(*) AS [Value]
FROM [dbo].[ImageValidationResults]
WHERE [IsDeleted] = 0 AND [Eligibility] = 'Not Eligible';

-- Eligibility Rate
SELECT 'Eligibility Rate (%)' AS Metric,
	ROUND(CAST(SUM(CASE WHEN [Eligibility] = 'Eligible' THEN 1 ELSE 0 END) AS FLOAT) / COUNT(*) * 100, 1) AS [Value]
FROM [dbo].[ImageValidationResults] WHERE [IsDeleted] = 0;

-- Local Matches (InventoryPool with Status = 'Matched')
SELECT 'Local Matches' AS Metric, COUNT(*) AS [Value]
FROM [dbo].[InventoryPool]
WHERE [IsDeleted] = 0 AND [Status] = 'Matched';

-- Match Rate of Eligible items
SELECT 'Match Rate of Eligible (%)' AS Metric,
	ROUND(CAST((SELECT COUNT(*) FROM [dbo].[InventoryPool] WHERE [IsDeleted] = 0 AND [Status] = 'Matched') AS FLOAT)
	/ NULLIF((SELECT COUNT(*) FROM [dbo].[InventoryPool] WHERE [IsDeleted] = 0), 0) * 100, 1) AS [Value];

-- Total Distance Saved (estimated: matched items * avg distance)
SELECT 'Total Distance Saved (km)' AS Metric,
	SUM(CASE
		WHEN ip.[Location] = 'Delhi' THEN 680
		WHEN ip.[Location] = 'Mumbai' THEN 450
		WHEN ip.[Location] = 'Bangalore' THEN 350
		WHEN ip.[Location] = 'Hyderabad' THEN 200
		WHEN ip.[Location] = 'Chennai' THEN 120
		WHEN ip.[Location] = 'Pune' THEN 150
		ELSE 100
	END) AS [Value]
FROM [dbo].[InventoryPool] ip
WHERE ip.[IsDeleted] = 0 AND ip.[Status] = 'Matched';

-- Total Cost Saved (estimated: distance-based)
SELECT 'Total Cost Saved (Rs.)' AS Metric,
	ROUND(SUM(CASE
		WHEN ip.[Location] = 'Delhi' THEN 3400.0
		WHEN ip.[Location] = 'Mumbai' THEN 2800.0
		WHEN ip.[Location] = 'Bangalore' THEN 2200.0
		WHEN ip.[Location] = 'Hyderabad' THEN 1400.0
		WHEN ip.[Location] = 'Chennai' THEN 1000.0
		WHEN ip.[Location] = 'Pune' THEN 1200.0
		ELSE 800.0
	END), 2) AS [Value]
FROM [dbo].[InventoryPool] ip
WHERE ip.[IsDeleted] = 0 AND ip.[Status] = 'Matched';

-- Total CO2 Saved (estimated: 0.021 kg per km)
SELECT 'Total CO2 Saved (kg)' AS Metric,
	ROUND(SUM(CASE
		WHEN ip.[Location] = 'Delhi' THEN 680 * 0.021
		WHEN ip.[Location] = 'Mumbai' THEN 450 * 0.021
		WHEN ip.[Location] = 'Bangalore' THEN 350 * 0.021
		WHEN ip.[Location] = 'Hyderabad' THEN 200 * 0.021
		WHEN ip.[Location] = 'Chennai' THEN 120 * 0.021
		WHEN ip.[Location] = 'Pune' THEN 150 * 0.021
		ELSE 100 * 0.021
	END), 2) AS [Value]
FROM [dbo].[InventoryPool] ip
WHERE ip.[IsDeleted] = 0 AND ip.[Status] = 'Matched';

-- Demand History coverage
SELECT 'DemandHistory Records' AS Metric, COUNT(*) AS [Value]
FROM [dbo].[DemandHistory] WHERE [IsDeleted] = 0;

-- Agent Recommendations by Agent
SELECT [AgentName], COUNT(*) AS RecommendationCount
FROM [dbo].[AgentRecommendations]
WHERE [IsDeleted] = 0
GROUP BY [AgentName]
ORDER BY [AgentName];

-- Summary by Location
SELECT
	ivr.[Location],
	COUNT(*) AS TotalReturns,
	SUM(CASE WHEN ivr.[Eligibility] = 'Eligible' THEN 1 ELSE 0 END) AS Eligible,
	(SELECT COUNT(*) FROM [dbo].[InventoryPool] ip
	 WHERE ip.[IsDeleted] = 0 AND ip.[Status] = 'Matched' AND ip.[Location] = ivr.[Location]) AS Matched
FROM [dbo].[ImageValidationResults] ivr
WHERE ivr.[IsDeleted] = 0
GROUP BY ivr.[Location]
ORDER BY ivr.[Location];

PRINT '========================================';
PRINT 'Verification Complete!';
PRINT '========================================';
GO


INSERT INTO [dbo].[Buyers] (Hub, Name, Zone, DistanceKm, Delivery, Score) VALUES
-- CHN
('CHN', 'Arjun Sharma', 'T. Nagar, Chennai', 2.1, '2h 30min', 96),
('CHN', 'Priya Krishnamurthy', 'Adyar, Chennai', 3.4, '3h 15min', 91),
('CHN', 'Ravi Sundar', 'Velachery, Chennai', 5.2, '4h 00min', 87),
('CHN', 'Meera Nair', 'Porur, Chennai', 7.8, '5h 30min', 82),
('CHN', 'Karthik Rajan', 'Tambaram, Chennai', 9.1, '6h 00min', 78),
-- BLR
('BLR', 'Rohan Mehta', 'Indiranagar', 1.8, '2h 00min', 94),
('BLR', 'Sneha Reddy', 'Koramangala', 3.1, '3h 00min', 88),
('BLR', 'Vikram Nair', 'Whitefield', 6.2, '4h 30min', 83),
('BLR', 'Anjali Singh', 'HSR Layout', 8.4, '5h 00min', 77),
('BLR', 'Deepak Kumar', 'Electronic City', 11.2, '6h 30min', 71),
-- MUM
('MUM', 'Rahul Joshi', 'Bandra', 2.5, '2h 45min', 91),
('MUM', 'Pooja Sharma', 'Andheri', 4.0, '3h 30min', 85),
('MUM', 'Amit Patel', 'Powai', 7.1, '5h 00min', 79),
('MUM', 'Kavita Desai', 'Thane', 9.3, '5h 45min', 74),
('MUM', 'Suresh Iyer', 'Navi Mumbai', 12.0, '7h 00min', 68),
-- DEL
('DEL', 'Aditya Kapoor', 'Connaught Place', 3.2, '3h 15min', 86),
('DEL', 'Neha Gupta', 'Lajpat Nagar', 5.8, '4h 30min', 79),
('DEL', 'Rajesh Verma', 'Dwarka', 9.0, '5h 30min', 71),
('DEL', 'Sunita Chauhan', 'Noida', 11.5, '6h 15min', 65),
('DEL', 'Manoj Tiwari', 'Gurgaon', 14.2, '7h 30min', 59),
-- HYD
('HYD', 'Srinivas Rao', 'Hitech City', 2.2, '2h 15min', 93),
('HYD', 'Lakshmi Prasad', 'Banjara Hills', 4.5, '3h 30min', 86),
('HYD', 'Venkat Reddy', 'Madhapur', 6.8, '4h 45min', 80),
('HYD', 'Padma Rao', 'Secunderabad', 9.5, '5h 30min', 73),
('HYD', 'Ramu Naidu', 'Kukatpally', 11.8, '6h 45min', 67);
