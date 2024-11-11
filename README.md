# Azure-Integrated ASP.NET Core Web Application
## Project Overview
This project is a demo ASP.NET Core web application designed to showcase the integration of Azure services. The application allows users to upload products with images, make purchases, and store various data securely in the cloud. It leverages Azure Functions to enhance scalability, cloud efficiency, and overall application performance.

### Key Features
- Upload product information and images.
- Store data in Azure Tables and Blob Storage.
- Process orders using Azure Queues.
- Upload and manage files with Azure File Share.
- Handle order processing using Azure Queue-triggered functions.
## Technologies Used
- ASP.NET Core
- Azure Functions
- Azure Tables
- Azure Blob Storage
- Azure File Share
- Azure Queues
## Azure Functions Implemented
1. AddEntityFunction: Adds product or user information to Azure Tables.
2. UploadImageFunction: Uploads product images to Azure Blob Storage.
3. UploadFileFunction: Uploads files such as contracts to Azure File Share.
4. SendToQueueFunction: Sends order details to an Azure Queue in JSON format.
5. ProcessOrderFunction: Processes orders from the Azure Queue when triggered.
